using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;
using Toggle = UnityEngine.UI.Toggle;

namespace Plugins.mitaywalle.UI.Editor
{
	[Overlay(typeof(SceneView), ID)]
	public sealed class SceneOverlayPreviewSelectable : ToolbarOverlay, ITransientOverlay
	{
		public const string ID = "UI Selectable";
		public bool visible => Selection.GetFiltered<Selectable>(SelectionMode.TopLevel).Length > 0;

		private static MethodInfo _onValidate;
		private static MethodInfo _doStateTransition;
		private static MethodInfo _togglePlayEffect;
		private static MethodInfo _OnValidate;
		private static MethodInfo _OnCanvasGroupChanged;
		private static FieldInfo _m_GroupsAllowInteraction;
		private static PropertyInfo _currentSelectionState;
		private static PropertyInfo _isPointerDown;
		private static PropertyInfo _isPointerInside;
		private static PropertyInfo _hasSelection;
		private static SelectionState? _selectionState;
		private static bool _isOn;
		private static Type _type;
		private static object[] _args1 = new object[1];
		private static object[] _args2 = new object[2];
		private HashSet<EditorToolbarToggle> _toggles = new();
		private static readonly string[] values =
		{
			$"{nameof(SelectionState)}.{SelectionState.Normal}",
			$"{nameof(SelectionState)}.{SelectionState.Highlighted}",
			$"{nameof(SelectionState)}.{SelectionState.Pressed}",
			$"{nameof(SelectionState)}.{SelectionState.Selected}",
			$"{nameof(SelectionState)}.{SelectionState.Disabled}",
			ToggleSelectableStateIsOn.id,
		};

		private static GameObject[] _last;

		public SceneOverlayPreviewSelectable() : base(values) { }

		public override void OnCreated()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;
			var type = typeof(Selectable);
			collapsedIcon = (Texture2D)EditorGUIUtility.IconContent("d_Selectable Icon").image;
			_togglePlayEffect = typeof(Toggle).GetMethod("PlayEffect", BindingFlags.Instance | BindingFlags.NonPublic);
			_onValidate = type.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
			_OnCanvasGroupChanged = type.GetMethod("OnCanvasGroupChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			_doStateTransition = type.GetMethod("DoStateTransition", BindingFlags.Instance | BindingFlags.NonPublic);
			_m_GroupsAllowInteraction = type.GetField("m_GroupsAllowInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
			_currentSelectionState = type.GetProperty("currentSelectionState", BindingFlags.Instance | BindingFlags.NonPublic);
			_isPointerDown = type.GetProperty("isPointerDown", BindingFlags.Instance | BindingFlags.NonPublic);
			_isPointerInside = type.GetProperty("isPointerInside", BindingFlags.Instance | BindingFlags.NonPublic);
			_hasSelection = type.GetProperty("hasSelection", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement { name = ID };

			// var root = new ToggleButtonGroup() { name = "Selectable" };
			// root.isMultipleSelection = false;
			// root.allowEmptySelection = true;

			root.style.alignContent = new StyleEnum<Align>(Align.Stretch);
			foreach (SelectionState state in Enum.GetValues(typeof(SelectionState)))
			{
				var toggle = new EditorToolbarToggle();

				toggle.icon = Icons.Get(state);
				toggle.tooltip = toggle.text = state.ToString();
				toggle.Q<Image>().style.alignSelf = new StyleEnum<Align>(Align.Auto);
				toggle.SetValueWithoutNotify(_selectionState == state);
				toggle.RegisterValueChangedCallback(_ =>
				{
					foreach (EditorToolbarToggle anyToggle in _toggles)
					{
						if (anyToggle != toggle)
						{
							anyToggle.SetValueWithoutNotify(false);
						}
					}

					OnClick(state);
				});

				_toggles.Add(toggle);
				root.Add(toggle);
			}

			var toggle2 = new EditorToolbarToggle("Inverted");
			toggle2.icon = Icons.isOn;
			toggle2.SetValueWithoutNotify(_isOn);
			toggle2.RegisterValueChangedCallback(value => { SetToggleClick(value.newValue); });

			root.Add(toggle2);

			return root;
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			if (Selection.GetFiltered<Selectable>(SelectionMode.Deep)?.Length == 0) return;

			//RebuildSelectablesVisual();
		}

		private void OnSelectionChanged()
		{
			RevertLast();
			RebuildSelectablesVisual();
			_last = Selection.gameObjects;
		}

		private static void RevertLast()
		{
			if (_last == null) return;
			if (_last.Length == 0) return;

			foreach (GameObject gameObject in _last)
			{
				if (!gameObject) continue;

				if (gameObject.TryGetComponent<Selectable>(out var selectable))
				{
					RebuildSelectablesVisual(selectable, null);

					if (selectable is Toggle toggle)
					{
						_args1[0] = true;
						_togglePlayEffect.Invoke(toggle, _args1);
					}
				}
			}
		}

		private static void OnClick(SelectionState? state)
		{
			_selectionState = _selectionState == state ? null : state;

			Debug.Log(_selectionState);

			RebuildSelectablesVisual();
			SceneView.lastActiveSceneView.Repaint();
		}

		private static void RebuildSelectablesVisual()
		{
			foreach (Selectable selectable in Selection.GetFiltered<Selectable>(SelectionMode.TopLevel))
			{
				RebuildSelectablesVisual(selectable);

				if (selectable is Toggle toggle)
				{
					SetToggleValue(toggle, _isOn);
				}
			}
		}

		private static void SetToggleClick(bool state)
		{
			_isOn = state;
			RebuildSelectablesVisual();
		}

		private static void SetToggleValue(Toggle toggle, bool state)
		{
			if (toggle.graphic == null)
				return;

			state = state != toggle.isOn;

#if UNITY_EDITOR
			if (!Application.isPlaying)
				toggle.graphic.canvasRenderer.SetAlpha(state ? 1f : 0f);
			else
#endif
				toggle.graphic.CrossFadeAlpha(state ? 1f : 0f, 0f, true);
		}

		private static void RebuildSelectablesVisual(Selectable selectable)
		{
			RebuildSelectablesVisual(selectable, _selectionState);
		}

		private static void RebuildSelectablesVisual(Selectable selectable, SelectionState? state)
		{
			if (state.HasValue)
			{
				_args2[0] = state.Value;
				switch (state)
				{
					case SelectionState.Normal:
					{
						_m_GroupsAllowInteraction.SetValue(selectable, true);
						_isPointerInside.SetValue(selectable, false);
						_isPointerDown.SetValue(selectable, false);
						_hasSelection.SetValue(selectable, false);
						break;
					}

					case SelectionState.Highlighted:
					{
						_m_GroupsAllowInteraction.SetValue(selectable, true);
						_isPointerInside.SetValue(selectable, true);
						_isPointerDown.SetValue(selectable, false);
						_hasSelection.SetValue(selectable, false);
						break;
					}

					case SelectionState.Pressed:
					{
						_m_GroupsAllowInteraction.SetValue(selectable, true);
						_isPointerInside.SetValue(selectable, true);
						_hasSelection.SetValue(selectable, true);
						_isPointerDown.SetValue(selectable, true);
						break;
					}

					case SelectionState.Selected:
					{
						_m_GroupsAllowInteraction.SetValue(selectable, true);
						_isPointerInside.SetValue(selectable, false);
						_hasSelection.SetValue(selectable, false);
						_isPointerDown.SetValue(selectable, true);
						break;
					}

					case SelectionState.Disabled:
					{
						_m_GroupsAllowInteraction.SetValue(selectable, false);
						_isPointerInside.SetValue(selectable, false);
						_isPointerDown.SetValue(selectable, false);
						_hasSelection.SetValue(selectable, false);
						break;
					}
				}

				_args2[0] = (int)state.Value;
			}
			else
			{
				_OnCanvasGroupChanged.Invoke(selectable, null);
				_isPointerInside.SetValue(selectable, false);
				_isPointerDown.SetValue(selectable, false);
				_hasSelection.SetValue(selectable, false);

				_args2[0] = (int)_currentSelectionState.GetValue(selectable);
				Debug.Log("Clear");
			}

			_args2[1] = true;
			for (int i = 0; i < 2; i++)
			{
				_doStateTransition.Invoke(selectable, _args2);
				if (selectable.targetGraphic)
				{
					selectable.targetGraphic.SetAllDirty();
				}
			}

			//Debug.Log((SelectionState)_currentSelectionState.GetValue(selectable));
		}

		/// <summary>
		/// An enumeration of selected states of objects
		/// </summary>
		public enum SelectionState
		{
			/// <summary>
			/// The UI object can be selected.
			/// </summary>
			Normal,

			/// <summary>
			/// The UI object is highlighted.
			/// </summary>
			Highlighted,

			/// <summary>
			/// The UI object is pressed.
			/// </summary>
			Pressed,

			/// <summary>
			/// The UI object is selected
			/// </summary>
			Selected,

			/// <summary>
			/// The UI object cannot be selected.
			/// </summary>
			Disabled,
		}

		[EditorToolbarElement("SelectionState.Normal", typeof(SceneView))]
		public sealed class ToggleSelectableStateNormal : StateToggle
		{
			public ToggleSelectableStateNormal() : base(SelectionState.Normal) { }
		}

		[EditorToolbarElement("SelectionState.Highlighted", typeof(SceneView))]
		public sealed class ToggleSelectableStateHighlighted : StateToggle
		{
			public ToggleSelectableStateHighlighted() : base(SelectionState.Highlighted) { }
		}

		[EditorToolbarElement("SelectionState.Pressed", typeof(SceneView))]
		public sealed class ToggleSelectableStatePressed : StateToggle
		{
			public ToggleSelectableStatePressed() : base(SelectionState.Pressed) { }
		}

		[EditorToolbarElement("SelectionState.Selected", typeof(SceneView))]
		public sealed class ToggleSelectableStateSelected : StateToggle
		{
			public ToggleSelectableStateSelected() : base(SelectionState.Selected) { }
		}

		[EditorToolbarElement("SelectionState.Disabled", typeof(SceneView))]
		public sealed class ToggleSelectableStateDisabled : StateToggle
		{
			public ToggleSelectableStateDisabled() : base(SelectionState.Disabled) { }
		}

		public abstract class StateToggle : EditorToolbarToggle
		{
			public StateToggle(SelectionState state)
			{
				icon = Icons.Get(state);
				tooltip = state.ToString();

				SetValueWithoutNotify(_selectionState == state);
				this.RegisterValueChangedCallback(newValue =>
				{
					if (newValue.newValue)
					{
						var found = GetFirstAncestorOfType<VisualElement>().Query<StateToggle>();
						foreach (var element in found.ToList())
						{
							if (element != this)
							{
								element.SetValueWithoutNotify(false);
							}
						}
					}

					OnClick(state);
					RebuildSelectablesVisual();
				});
			}
		}

		[EditorToolbarElement(id, typeof(SceneView))]
		public sealed class ToggleSelectableStateIsOn : EditorToolbarToggle
		{
			public const string id = "Toggle.isOn";

			public ToggleSelectableStateIsOn()
			{
				icon = Icons.isOn;
				tooltip = "isOn inverted";
				SetValueWithoutNotify(_isOn);
				this.RegisterValueChangedCallback(newValue => SetToggleClick(newValue.newValue));
			}
		}
	}

	public class Icons
	{
		private const string NormalText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAGlmlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMjoxMDo0OCswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDI6MTA6NDgrMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6ZDZjMTk3YTAtZTY2NS1lMTQ2LTg5NzAtYWEwNGQzYmFhZDJiIiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6N2ViY2RlNmYtMjEyYS02NzRhLWIwY2UtN2JhY2ZiNjFmNzg0IiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAyOjAwOjQxKzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjdlNDZmMGE3LThkZWEtMzk0MS1iNDE5LWE0ODI3NThjN2YxOCIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJzYXZlZCIgc3RFdnQ6aW5zdGFuY2VJRD0ieG1wLmlpZDpkNmMxOTdhMC1lNjY1LWUxNDYtODk3MC1hYTA0ZDNiYWFkMmIiIHN0RXZ0OndoZW49IjIwMjUtMDUtMDZUMDI6MTA6NDgrMDM6MDAiIHN0RXZ0OnNvZnR3YXJlQWdlbnQ9IkFkb2JlIFBob3Rvc2hvcCAyNS42IChXaW5kb3dzKSIgc3RFdnQ6Y2hhbmdlZD0iLyIvPiA8L3JkZjpTZXE+IDwveG1wTU06SGlzdG9yeT4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz5niTAFAAAAbklEQVQ4je2TsQrAIAxEX6rQoVP//y8LDoVCumhJRUVx6dBbPPB4FySKqjIjb/wOhOg1OwEcsEV/AQeAmAkCcAJrDDtTkEJigJIDNAu09GR95bKlV8nS2VjVD/gCoLQHvctUBAz/LAsYak6afoMbmjoVIBXmPxsAAAAASUVORK5CYII=";
		private const string HighlightedText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAGlmlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMTo1Njo0NiswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMzoxNjozMCswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDM6MTY6MzArMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6NWExMzczZDYtMjYwZi05YzRmLThmZmItOTYzNDQzNjhiMWJlIiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6MmVhZDNmZWEtOTc5MS04MzQwLThlMzYtYmFiNjNjODNmYzUxIiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6OTdhMDI2YTEtMWMyOC01MjQ1LTlkMDItNmQ1NzQ5YmNhZDVmIiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6OTdhMDI2YTEtMWMyOC01MjQ1LTlkMDItNmQ1NzQ5YmNhZDVmIiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAxOjU2OjQ2KzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjAzY2Y2MTM4LWY1ZjQtMDU0MS1iNDgyLTJjMDM1MGY3NWFlYiIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMTo1Njo0NiswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJzYXZlZCIgc3RFdnQ6aW5zdGFuY2VJRD0ieG1wLmlpZDo1YTEzNzNkNi0yNjBmLTljNGYtOGZmYi05NjM0NDM2OGIxYmUiIHN0RXZ0OndoZW49IjIwMjUtMDUtMDZUMDM6MTY6MzArMDM6MDAiIHN0RXZ0OnNvZnR3YXJlQWdlbnQ9IkFkb2JlIFBob3Rvc2hvcCAyNS42IChXaW5kb3dzKSIgc3RFdnQ6Y2hhbmdlZD0iLyIvPiA8L3JkZjpTZXE+IDwveG1wTU06SGlzdG9yeT4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz54PexkAAAAxElEQVQ4jaWRrXICMRhFz/IjthqNQVTzCLUYXEU1HsOD9E3qUJgaajB1q8EwfYNOxcEkMyGzmy1wZ77J5OeefLmpVB7R4CF3C0Bgewugyp4QJ1PgdE8Hr2H8iBckezUwA+ZXDjWvqFUYG/XotSbxfBtgYb/WJQDquQewj2fzEKOegaYnvwpglC3ugDHwUzD+AZ9dIZb0pdahhl0ZPKkv6kb9zgCo7+pvAuwMMdYyAQxbuuoFEIxvofW7AGkdEvPewjf+WxdyfcuEG+Nf1QAAAABJRU5ErkJggg==";
		private const string PressedText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAFyWlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMTo1Njo0NiswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMTo1Njo0NiswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDE6NTY6NDYrMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6MDNjZjYxMzgtZjVmNC0wNTQxLWI0ODItMmMwMzUwZjc1YWViIiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6ZmFkM2EzMzEtM2U2Ny1kMTQzLWEyNWEtMTIxMmUzZjU3MjM1IiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6OTdhMDI2YTEtMWMyOC01MjQ1LTlkMDItNmQ1NzQ5YmNhZDVmIiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6OTdhMDI2YTEtMWMyOC01MjQ1LTlkMDItNmQ1NzQ5YmNhZDVmIiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAxOjU2OjQ2KzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjAzY2Y2MTM4LWY1ZjQtMDU0MS1iNDgyLTJjMDM1MGY3NWFlYiIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMTo1Njo0NiswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDwvcmRmOlNlcT4gPC94bXBNTTpIaXN0b3J5PiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/PoWuubMAAADUSURBVDiNnZGhbkJBFETPAyqorsZUoPsJtZg6RHV9TT+EP6nEYGqoqcGhwTT8QVNxavaml5flPWCSyU02O3NnZxuVBIGGCzBIQpLYyt0qRhXh8pIUTXlCRI/NE2B/jkE8ITbOy3xvnQOMgXvg4chBDZqm6kuZW3XnMe5Clw2CM/vx2mWA+t1jsI67UWIbU2Db018D/98YWAE3wKFD+At81EqkK7P6qY4Lh6c6uFUf1Td10zJAXag/yfBkicGnZDCspOo1oAifS/SrDDK/knhtxzeejT8Dm9CICLEQBAAAAABJRU5ErkJggg==";
		private const string SelectedText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAGlmlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMjoxMTo0NSswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDI6MTE6NDUrMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6NGVhYmMxZTUtNzUyOC1mYjQ3LWFkZTAtYWViOTRkODM5Mjc5IiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6NmU1YWQyNDMtOTViYS04ZTQzLWFlMDYtNDA1YTg2MTI2MWNlIiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAyOjAwOjQxKzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjdlNDZmMGE3LThkZWEtMzk0MS1iNDE5LWE0ODI3NThjN2YxOCIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJzYXZlZCIgc3RFdnQ6aW5zdGFuY2VJRD0ieG1wLmlpZDo0ZWFiYzFlNS03NTI4LWZiNDctYWRlMC1hZWI5NGQ4MzkyNzkiIHN0RXZ0OndoZW49IjIwMjUtMDUtMDZUMDI6MTE6NDUrMDM6MDAiIHN0RXZ0OnNvZnR3YXJlQWdlbnQ9IkFkb2JlIFBob3Rvc2hvcCAyNS42IChXaW5kb3dzKSIgc3RFdnQ6Y2hhbmdlZD0iLyIvPiA8L3JkZjpTZXE+IDwveG1wTU06SGlzdG9yeT4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz6ZGVJIAAAAc0lEQVQ4je2TsQ6AIAxEX9XNSf7/K00YTEzqAqQQlSYsDt5CU467Qg5RVUawmHoDYqq1WQFmYE31CewAYiaIwAGEjmk+IK2A5qYDhWuvIGbzDWKNJqfjI36BLwjYHORweMJUgtRO4PlZFecuiT1UvOE3uADl2Rggldq8OgAAAABJRU5ErkJggg==";
		private const string DisabledText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAGlmlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMjozMzoxMSswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDI6MzM6MTErMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6NWY5MTcwOWMtYjkxNy03NDRjLWJjNzEtM2NmM2M5NTE4YjRmIiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6N2ViY2RlNmYtMjEyYS02NzRhLWIwY2UtN2JhY2ZiNjFmNzg0IiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6MGI3MWQzZDYtMDhkOS1kOTRkLWE1ODItNGY1ZTg1ZDg3YTE4IiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAyOjAwOjQxKzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjdlNDZmMGE3LThkZWEtMzk0MS1iNDE5LWE0ODI3NThjN2YxOCIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMjowMDo0MSswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJzYXZlZCIgc3RFdnQ6aW5zdGFuY2VJRD0ieG1wLmlpZDo1ZjkxNzA5Yy1iOTE3LTc0NGMtYmM3MS0zY2YzYzk1MThiNGYiIHN0RXZ0OndoZW49IjIwMjUtMDUtMDZUMDI6MzM6MTErMDM6MDAiIHN0RXZ0OnNvZnR3YXJlQWdlbnQ9IkFkb2JlIFBob3Rvc2hvcCAyNS42IChXaW5kb3dzKSIgc3RFdnQ6Y2hhbmdlZD0iLyIvPiA8L3JkZjpTZXE+IDwveG1wTU06SGlzdG9yeT4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz74vP3tAAAAiElEQVQ4jdWTvQ6DMAyEP36WbjxmV7Y8QhaeFqkLVGbARCegQBUWTrJ8Tk4Xx0oKMyMHtfAG+Di3VQaogJfzL9CvDd6eB6BzHjxHYARa0cf5CLMlgvCzSFrtYEHYWVNELcpfG1dNyiPVFajBWeuKpL21g4fOYO8d/HOVjYE+3aM6ocj9jdlDnAB8pEjGQtr/gwAAAABJRU5ErkJggg==";
		private const string isOnText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAE7mlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczpkYz0iaHR0cDovL3B1cmwub3JnL2RjL2VsZW1lbnRzLzEuMS8iIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIiB4bWxuczpzdEV2dD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlRXZlbnQjIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMjoyNzozNiswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMjoyNzozNiswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDI6Mjc6MzYrMDM6MDAiIGRjOmZvcm1hdD0iaW1hZ2UvcG5nIiB4bXBNTTpJbnN0YW5jZUlEPSJ4bXAuaWlkOmI3N2FmMmI1LTE3OTQtZDg0OS04YWZjLTg4YzYwN2E2ZjM3NCIgeG1wTU06RG9jdW1lbnRJRD0ieG1wLmRpZDpiNzdhZjJiNS0xNzk0LWQ4NDktOGFmYy04OGM2MDdhNmYzNzQiIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDpiNzdhZjJiNS0xNzk0LWQ4NDktOGFmYy04OGM2MDdhNmYzNzQiIHBob3Rvc2hvcDpDb2xvck1vZGU9IjMiPiA8eG1wTU06SGlzdG9yeT4gPHJkZjpTZXE+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJjcmVhdGVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOmI3N2FmMmI1LTE3OTQtZDg0OS04YWZjLTg4YzYwN2E2ZjM3NCIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMjoyNzozNiswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIi8+IDwvcmRmOlNlcT4gPC94bXBNTTpIaXN0b3J5PiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/PiyMH0AAAAB8SURBVDiNrZKxCoAwDEQTcakf5QeLxc3F3xIcz6kSYpO21oNAW3KPHA0DoB4NXW4FQEOdycQiQmsW1hOUtBPR9HoFkMrTkukjAFWAKHrWEuAAEAxzVOAsIIj75pjdCKQqZ3YBEmKZn57R+LLqnfh1la8G35wOcpW7J/ikG5GnaosoiGWXAAAAAElFTkSuQmCC";
		private const string SelectableText = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAFyWlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZGJhM2RhMywgMjAyMy8xMi8xMy0wNTowNjo0OSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIiB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyNS0wNS0wNlQwMTo1NToxMiswMzowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNS0wNS0wNlQwMTo1NToxMiswMzowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjUtMDUtMDZUMDE6NTU6MTIrMDM6MDAiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6NzlhZWRjOGQtMjk4NS03NjQyLTk4MWUtNDY1OWNlMzFkOTMwIiB4bXBNTTpEb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6MjgzY2FiZmQtMGVjYy02ZDRiLWI1MzMtNTZlOGQzZTg0YTFkIiB4bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ9InhtcC5kaWQ6MmYzOTc4NmEtNzQ3Ny1lZDQyLTg0ZGQtMzhlOTQ1MjRjMjA5IiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyI+IDx4bXBNTTpIaXN0b3J5PiA8cmRmOlNlcT4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNyZWF0ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6MmYzOTc4NmEtNzQ3Ny1lZDQyLTg0ZGQtMzhlOTQ1MjRjMjA5IiBzdEV2dDp3aGVuPSIyMDI1LTA1LTA2VDAxOjU1OjEyKzAzOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuNiAoV2luZG93cykiLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjc5YWVkYzhkLTI5ODUtNzY0Mi05ODFlLTQ2NTljZTMxZDkzMCIgc3RFdnQ6d2hlbj0iMjAyNS0wNS0wNlQwMTo1NToxMiswMzowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjYgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDwvcmRmOlNlcT4gPC94bXBNTTpIaXN0b3J5PiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/PngINx0AAACvSURBVDiNpZLBDcIwDEV/oRMwDYuFE9euwixsAhcYwFFjDpjoY+wWxJe+FDvOc6xkUFX8ow2tK1kA3AA0WwuASwQY6AZC+ZHgDcAMYOsa9sIOCxoUO9SyEUYXqwNNFqcQD4g0Gfjj+s+Wqi9XcrOcUK5QbXcEEALUNQgHkgCqgx8ywNUONo1H4AYlAkT2Y83U4PgLYJ/VxE/zrhOAc7bJXznSHcBuqWANsKpvRljUA32ha00zw3UOAAAAAElFTkSuQmCC";

		public static Texture2D Normal => Create(NormalText);
		public static Texture2D Pressed => Create(PressedText);
		public static Texture2D Highligted => Create(HighlightedText);
		public static Texture2D Selected => Create(SelectedText);
		public static Texture2D Disabled => Create(DisabledText);
		public static Texture2D isOn => Create(isOnText);
		public static Texture2D Selectable => Create(SelectableText);

		public static Texture2D Get(SceneOverlayPreviewSelectable.SelectionState state)
		{
			return state switch
			{
				SceneOverlayPreviewSelectable.SelectionState.Normal => Normal,
				SceneOverlayPreviewSelectable.SelectionState.Highlighted => Highligted,
				SceneOverlayPreviewSelectable.SelectionState.Pressed => Pressed,
				SceneOverlayPreviewSelectable.SelectionState.Selected => Selected,
				SceneOverlayPreviewSelectable.SelectionState.Disabled => Disabled,
				_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
			};
		}

		private static Texture2D Create(string text)
		{
			var t = new Texture2D(16, 16);
			t.LoadImage(Convert.FromBase64String(text));
			return t;
		}
	}
}
