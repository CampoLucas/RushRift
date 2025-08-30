# EntityModel
**Type:** `class` | **Generic:** `<TData>` where `TData : EntityModelSO` | **Implements:** [IModel](IModel.md) | **Namespace:** `Game.Entities`

### Description
Represent the runtime data and logic of an entity. Each `EntityModel` instance is created from a `ScriptableObject` ([EntityModelSO](EntityModelSO.md)) as a runtime proxy. It manages **components** and handles **update cycles**.
Key Features:
- Stores entity components in a type-indexed dictionary for quick lookups.
- Manages `Update`, `LateUpdate` and `FixedUpdate` via `ISubject<float>` observers.
- Supports dynamic addition and removal of components at runtime.
- Implements `IDisposable` for clean removal of components and release of resources.

### Public Methods
| Method                | Description                                                                                                      |
| --------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `Init`                | Initializes the model with a reference to its controller. Override for custom logic.                             |
| `Update`              | Notifies all attached components that implement Update logic. It also passes the delta time as a parameter.      |
| `LateUpdate`          | Notifies all attached components that implement LateUpdate logic. It also passes the delta time as a parameter.  |
| `FixedUpdate`         | Notifies all attached components that implement FixedUpdate logic. It also passes the delta time as a parameter. |
| `TryGetComponent`     | Attempts to retrieve a component of type `TComponent`. Returns `true` if found.                                  |
| `TryAddComponent`     | Adds a component if it doesn’t already exist. Attaches its observers to the appropriate subjects.                |
| `RemoveComponent`     | Removes a component and optionally disposes it. Detaches its observers from subjects.                            |
| `HasComponent`        | Checks if the model contains a component of type `TComponent`.                                                   |
| `RemoveAllComponents` | Removes and disposes all components, detaching all observers.                                                    |
| `OnDraw`              | Calls `OnDraw` for all components for editor debug rendering.                                                    |
| `OnDrawSelected`      | Calls `OnDrawSelected` for all components when the entity is selected in the editor.                             |
### Notes
- Components can implement optional update methods (`Update`, `LateUpdate`, `FixedUpdate`). When a component is added, observers are attached only for the update methods it implements. This prevents redundant calls for components that don’t need certain updates, keeping runtime performance efficient.
- The model allows entities to dynamically **change behavior at runtime** by adding or removing components without modifying the Controller or View.
- Provides decoupling between entity data/logic and Unity’s MonoBehaviour system.

<div style="display: flex; justify-content: space-between;">
  <a href="../Controller/Example%20Usage.md">← Previous Page</a>
  <a href="EntityModelSO.md">Next Page →</a>
</div>