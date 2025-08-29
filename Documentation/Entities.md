# Custom Entity MVC + Component System
## Overview
This system is a hybrid of **Entity-Component System (ECS)** and **Object-Oriented Programing (OOP)** implemented in Unity. It separates entity data, behavior and visual info:
1. **Model** - Stores data and components, manages runtime logic, handles updates.
2. **View** - MonoBehaviour that Handles all the visual representation of the entity (e.g., animations, VFX, etc.)
3. **Controller** - MonoBehaviour that is in charge of the **Model** and **View** at runtime, serves as the main interface for other systems.
The system uses **runtime proxies** for the **Model** so that the ScriptableObject can generate fully functional and disposable instances. Components are standard C# classes implementing the `IEntityComponent`, which the model manages dynamically.

The design aims to be **controller-agnostic:** any system interacting with an entity only needs an `IController` interface without knowing the specific implementation.

----

## Controller

`EntityController`
**Type:** `ObserverComponent`
**Purpose:** Main runtime entry point for an entity. Initializes the model and executes the components.
**Key Features:**
- Hold references to `Origin`, `Joints`, `EntityModelSO` and `Animators`
- Manages a finite state machine (`EntityStateMachine`)
- Forwards Unity's `Update`, `LateUpdate` and `FixedUpdate` calls to the model.
- Has `Observer` and `Subject` functionality.
- Implements `IDisposable` to clean up proxies and FSM.

