# Scene Hierarchy Guidelines
This document defines the standard order and organization of the **Unity Scene Hierarchy** in this project.
The goal is to keep the scenes clean, predictable and easy to navigate as the project grows.

## Hierarchy Order
Always structure the root object in the following order:
1. **Managers**
	- Prefab where managers are stored. This way, when a manager is added or removed, every level is automatically updated.
	- **Examples:** `LevelManager`, `AudioManager`, `InputManager`.
2. **Camera**
	- The main camera, Cinemachine rigs, and any attached scripts.
3. **UI**
	- All user interface elements that are in **screen space**.
4. **Level**
	Contains level-specific elements, divided into the following subcategories:
	- **Lights** 
		- Scene lighting, light probes, reflection probes, post-processing volumes.
	- **Static**
		- Everything that doesn’t move or animate.
		- ❌ Does not include objects with any kind of animation, even if they don’t change position (this includes shader/material animations).
		- This prefab and all its content should be marked as **Static** in the Inspector for performance and lightmap baking.
	- **Dynamic**
		- Level elements with animations or runtime changes.
		- **Examples:** Moving platforms, doors, rotating fans, animated props
	- **Entities**
		- All characters and actors.
		- **Examples:** `Player`, `Enemies`, `Spawners`
	- **Props**
		- Interactable objects the player can use or affect.
		- **Examples:** jump-pads, terminals, etc.
		- ⚠ Does not include purely decorative props (those go under Static or Dynamic).

## Rule of Thumb
- **Static folder:**
    - Objects under `Static` should have the Unity **Static** flag enabled for batching and baked lighting.
- **Decorative vs Interactable:**
    - Decorative → `Static` or `Dynamic`.
    - Interactable → `Props`.
- **UI placement:**
    - Screen space → `UI`.
    - World space → `Dynamic` (unless it’s a global UI element like a HUD).

## Example Layout
```
├── Managers
│    ├── GameManager
│    └── AudioManager
│
├── Camera
│    └── MainCamera
│
├── UI
│    ├── HUD
│    └── PauseMenu
│
├── Level
│    ├── Lights
│    │    ├── DirectionalLight
│    │    └── LightProbes
│    │
│    ├── Static
│    │    ├── Terrain
│    │    └── Buildings
│    │
│    ├── Dynamic
│    │    ├── MovingPlatform
│    │    └── RotatingFan
│    │
│    ├── Entities
│    │    ├── Player
│    │    └── Enemy_Grunt
│    │
│    └── Props
│         ├── Pickup_Health
│         └── Switch_Door
```

