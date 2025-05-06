# Unity3d Selectable Preview Overlay
Unity3d SceneView Overlay to preview Graphic states of UI.Selectable (Button, Toggle etc) 

![selectableOverlay](https://github.com/user-attachments/assets/680a86c0-9de2-4b9d-8f7a-12421a89a524)

## Installation
- copy [SceneOverlayPreviewSelectable.cs](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/blob/main/SceneOverlayPreviewSelectable.cs) to folder "Project/Assets/../Editor/.."
## Summary
- single-file script
- dockable overlay
- hides if no Selectable in selection
- not change any Serialized (saved) values, only temporal
- preview all built-in animations: color, sprite swap, animation,
- preview Custom animations based on UI.Selectable.DoStateTransition 
## Support Any Selectables
- Button
- Toggle (Include isOn graphic)
- Slider
- Scrollbar
- Any custom Selectable, animated through [UI.Selectable.DoStateTransition()](https://docs.unity.cn/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Selectable.html#UnityEngine_UI_Selectable_DoStateTransition_UnityEngine_UI_Selectable_SelectionState_System_Boolean_)

## Known Issues
- [ ] [#1 Deselection not revert state](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/issues/1)

- [ ] [#2 Clicks on overlay buttons change visual not immediately](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/issues/2)

## Screenshots
Normal state no overrides, undocked Panel view

![image](https://github.com/user-attachments/assets/1061f466-e857-4d25-a2a6-20879cef8b7a)

Toggle.isOn is inverted

![image](https://github.com/user-attachments/assets/2442032d-73cf-44c0-9c29-7f3016b1e0c8)

Docked toolbar view

![image](https://github.com/user-attachments/assets/225fe27a-8d19-49ca-9383-2e83f013ad94)

Button state is overriden to Selected, also scaled by custom script based on UI.Selectable.DoStateTransition()

![image](https://github.com/user-attachments/assets/b4110b57-6f0f-417f-9d14-888a659d5873)

All selected Selectables states overriden to Selected

![image](https://github.com/user-attachments/assets/839f5e49-dcdb-4283-ac97-9036f37503a4)

