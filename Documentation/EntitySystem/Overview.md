# Custom Entity MVC + Component System
## Overview
This system is a hybrid of **Entity-Component System (ECS)** and **Object-Oriented Programing (OOP)** implemented in Unity. It separates **data**, **behavior** and **visual representation** of entities using a Model-View-Controller (MVC) pattern:
1. **Model** - Stores entity data and components, manages runtime logic and handles its updates.
2. **View** - A `MonoBehaviour` responsible for all the visual elements (e.g., animations, VFX, etc.)
3. **Controller** - A `MonoBehaviour` that's' in charge of the **Model** and **View** at runtime, serves as the main interface for other systems.
The system uses **runtime proxies** for the **Model** so that the ScriptableObject can generate fully functional and disposable instances. Components are standard C# classes implementing the `IEntityComponent`, which the model manages dynamically.

The design aims to be **controller-agnostic:** any system interacting with an entity only needs the `IController` interface without knowing the specific implementation.
## Why this approach?
Unity's default `MonoBehaviour` component system simple, but it tends to mix **data**, **logic** and **presentation** in a single class. This makes reusability and runtime flexibility harder.
* ECS is extremely flexible and performant, but implementing a full ECS framework from scratch in Unity can be overkill.
* A pure OOP approach (standard MonoBehaviours) is easy to use but harder to decouple.
This hybrid system:
- Keeps the **data-driven flexibility** of ECS components.
- Maintains the **familiar workflow** of Unity's MonoBehaviours.
- Enforces **strict separation** of data, logic and visuals.
## Benefits
- **Clear separation of concerns** - Data (Model), behavior/control (Controller) and the visuals (View) are clearly split.
- **Runtime flexibility** - Entities can add or remove components dynamically.
- **Interface-driven design** - Other systems only depend on `IController`, reducing coupling.
- **Reusable logic** - Components can be shared across different entity types without modifications.

## More Details
- [EntityController](EntityController.md)
- [EntityModel](EntityModel.md)
- [EntityView](EntityView.md)
- Custom Components
