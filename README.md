# Unity3d Selectable Preview Overlay
Unity3d SceneView Overlay to preview Graphic states of UI.Selectable (Button, Toggle etc) 

[Youtube view usage exampe](https://youtu.be/Y32J_p7ZOU4?si=b2L-mOwXcLBg4SY4)
![selectableOverlay2](https://github.com/user-attachments/assets/419b50d8-11ab-4150-9c5b-2fb770c73135)

Panel / horizontal docked / vertical docked / collapsed

![image](https://github.com/user-attachments/assets/c6db07fb-74f3-4c1b-911f-d28972dedabc)
![image](https://github.com/user-attachments/assets/cb7c8f9c-944f-4a03-b38b-803eb7f4be6d)
![image](https://github.com/user-attachments/assets/f70aa3ef-91a6-4fcb-b902-fd327f8973f3)
![image](https://github.com/user-attachments/assets/92c5fbe7-6608-48e3-b3fc-ed4c36f4d04b)

## Installation
- copy [SceneOverlayPreviewSelectable.cs](https://github.com/mitay-walle/com.mitay-walle.ui-selectable-preview-overlay/blob/main/SceneOverlayPreviewSelectable.cs) to folder "Project/Assets/../Editor/.."
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

## Screenshots
Normal state no overrides, undocked Panel view

![image](https://github.com/user-attachments/assets/1061f466-e857-4d25-a2a6-20879cef8b7a)

Toggle.isOn is inverted

![image](https://github.com/user-attachments/assets/2442032d-73cf-44c0-9c29-7f3016b1e0c8)

Docked toolbar view

![image](https://github.com/user-attachments/assets/225fe27a-8d19-49ca-9383-2e83f013ad94)

Button state is overriden to Pressed, also scaled by custom script based on UI.Selectable.DoStateTransition()

![image](https://github.com/user-attachments/assets/b4110b57-6f0f-417f-9d14-888a659d5873)

All selected Selectables states overriden to Selected

![image](https://github.com/user-attachments/assets/839f5e49-dcdb-4283-ac97-9036f37503a4)

