# EntityController
**Type:** `class` | **Inherits from:** [`ObserverComponent`](Documentation/5_Design%20Patterns/Observers/ObserverComponent.md) | **Implements:** [`IController`](IController.md) | **Implemented in:** `Game.Entities`

### Description
Main entry point for an entity. Initializes the model, manages runtime components and forwards Unity lifecycle calls to the model.
**Key Features:**
- Hold references to `Origin`, `Joints`, `EntityModelSO` and `Animators`
- Manages a finite state machine (`EntityStateMachine`)
- Forwards Unity's `Update`, `LateUpdate` and `FixedUpdate` calls to the model.
- Implements `Observer` and `Subject` functionality.
- Implements `IDisposable` to clean up proxies and FSM.
### Properties
| Property | Description                                                |
| -------- | ---------------------------------------------------------- |
| `Origin` | Reference to the entity's `transform`.                     |
| `Joints` | Collection of transforms that represent the entity joints. |
### Static Variables
| Name      | Description                     |
| --------- | ------------------------------- |
| `DESTROY` | The ID of the destroy observer. |
### Serialized Variables
| Name       | Description                                        |
| ---------- | -------------------------------------------------- |
| `model`    | `ScriptableObject` used to create the model proxy. |
| `joints`   | Assignable reference to the joints of the entity.  |
| `animator` | References to the animators of the entity.         |
> [!NOTE] 
> The animators aren't supposed to be inside the controller, instead should be in the **View** component.
### Public Methods
| Method           | Description                                                                    |
| ---------------- | ------------------------------------------------------------------------------ |
| `GetModel`       | Returns the runtime model instance.                                            |
| `GetView`        | Returns the runtime view instance.                                             |
| `MoveDirection`  | Abstract method to provide input direction, must be implemented by subclasses. |
| `TryGetObserver` |                                                                                |
| `TryGetSubject`  |                                                                                |
| `OnNotify`       |                                                                                |
| `Dispose`        | Cleans up model, view, FSM, and stops all coroutines.                          |
### Notes
- The controller acts as the main interface for other systems via the `IController` interface.
- It ensures that the Model is decouple from Unity's MonoBehaviour system, allowing runtime proxies and component-based updates.
- Handles forwarding of  Unity lifecycle events, but the actual logic happens in the Model proxy and it's components.
- Go to example [Here](Example%20Usage.md)

[← Previous Page](../Overview.md) | [Next Page →](IController.md)
