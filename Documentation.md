
# Selectable Preview Overlay. Documentation

# Quick Start Guide

1. Install package from [Github](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/tree/main) to project
2. Open project
3. create any [UI.Selectabe](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Selectable.html) (Button, Toggle, Slider, Scrollbar, or custom)
4. Configure any **Transition** to it (Colors, Sprite Swap, Animator)
5. Select this object in Hierarchy-window
6. Open Scene-window
7. find UI Selectable Overlay
8. press any toggle in it
9. Result: Selectable will force Transition to selected state
 
Example: 

![selectableOverlay2](https://github.com/user-attachments/assets/419b50d8-11ab-4150-9c5b-2fb770c73135)
## Summary
- single-file script
- dockable overlay
- hides if no Selectable in selection
- not change any Serialized (saved) values, only temporal
- preview all built-in animations: color, sprite swap, animation,
- preview Custom animations based on UI.Selectable.DoStateTransition
- Tested witn Unity 6, Unity 2022
## Support Any Selectables
- Button
- Toggle (Include isOn graphic)
- Slider
- Scrollbar
- Any custom Selectable, animated through [UI.Selectable.DoStateTransition()](https://docs.unity.cn/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Selectable.html#UnityEngine_UI_Selectable_DoStateTransition_UnityEngine_UI_Selectable_SelectionState_System_Boolean_)

## IMPORTANT
 1. Overlay hide if no Selected Selectables
![](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/blob/main/SelectablesOverlayDoc.gif?raw=true)
 2. Selectable (and it's prefab or scene) is marked dirty when switching (or forcing) states
![](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/blob/main/SelectablesOverlayDoc2.gif?raw=true)
## Overlay Content (Toggles)

Overlay has different toggles, that force specific Selectable state

- If no toggles enabled in overlay, Selectable component work as is
- if any toggle is pressed then state of selected Selectables is forced

Next toggles **work with any Selectable** .only toggle one can be active at once
1. Normal (interactive, not selected, no pointer upon, no pointer pressed )
2. Highlighted (interactive, pointer upon, no pointer pressed )
3. Pressed (interactive, pointer pressed )
4. Selected ((interactive, selected)
5. Disabled (not interactive)

**Work with [UI.Toggle](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Toggle.html) only** , can be active independently:
6. Inverted - If enabled, checkmark graphic hide with isOn = true and vice versa 

## Overlay Presentation
Overlay can display in next modes:
1. Panel
![image](https://github.com/user-attachments/assets/c6db07fb-74f3-4c1b-911f-d28972dedabc)

9. Horizontal toolbar 
![image](https://github.com/user-attachments/assets/cb7c8f9c-944f-4a03-b38b-803eb7f4be6d)

10. Vertical 
![image](https://github.com/user-attachments/assets/f70aa3ef-91a6-4fcb-b902-fd327f8973f3)
11. Collapsed
![image](https://github.com/user-attachments/assets/92c5fbe7-6608-48e3-b3fc-ed4c36f4d04b)
Mode can be switched by docking Overlay to Scene window sidebars, or by overlay -> right mouse click -> mode
![](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/blob/main/SelectablesOverlayDoc3.png?raw=true)
