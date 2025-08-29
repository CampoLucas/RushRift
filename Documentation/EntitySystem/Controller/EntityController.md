# EntityController
**Type:** `class` | **Inherits from:** `ObserverComponent` | Implements: [IController](IController.md) | **Implemented in:** `Game.Entities`

### Description
Main entry point for an entity. Initializes the model and executes it's components.
**Key Features:**
- Hold references to `Origin`, `Joints`, `EntityModelSO` and `Animators`
- Manages a finite state machine (`EntityStateMachine`)
- Forwards Unity's `Update`, `LateUpdate` and `FixedUpdate` calls to the model.
- Has `Observer` and `Subject` functionality.
- Implements `IDisposable` to clean up proxies and FSM.
### Properties
| Property | Description                                                |
| -------- | ---------------------------------------------------------- |
| Origin   | Reference to the entity's `transform`.                     |
| Joints   | Collection of transforms that represent the entity joints. |
### Static Variables
| Name    | Description                     |
| ------- | ------------------------------- |
| DESTROY | The ID of the destroy observer. |
### Serialized Variables
| Name     | Description                                        |
| -------- | -------------------------------------------------- |
| model    | `ScriptableObject` used to create the model proxy. |
| joints   | Assignable reference to the joints of the entity.  |
| animator | References to the animators of the entity.         |
> [!NOTE] 
> The animators aren't supposed to be inside the controller, instead should be in the **View** component.
### Public Methods
| Method           | Description                                                         |
| ---------------- | ------------------------------------------------------------------- |
| GetModel()       | Exposes the runtime model instance.                                 |
| GetView()        | Exposes the view reference.                                         |
| MoveDirection()  | Used to provide input direction (must be implemented by subclasses) |
| TryGetObserver() |                                                                     |
| TryGetSubject()  |                                                                     |
| OnNotify()       |                                                                     |
| Dispose()        |                                                                     |
