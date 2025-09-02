# IController
**Type:** `interface` | **Implements:** `IDisposable`, [`IObserver`](IObserver.md) | **Implemented in:** `Game.Entities`

### Description
Used to abstract interactions with any type of controller, allowing systems (AI, animations, input, etc.) to work without knowing the specific controller type.

### Properties
| Property | Description                                                                                |
| -------- | ------------------------------------------------------------------------------------------ |
| `Origin` | Reference to the entity's `transform`.                                                     |
| `Joints` | Collections of transforms, useful for making custom attacks that use different bone joints |
### Methods
| Method           | Description                                                 |
| ---------------- | ----------------------------------------------------------- |
| `GetModel`       | Returns the model instance associated with this controller. |
| `GetView`        | Returns the view instance associated with this controller.  |
| `MoveDirection`  | Movement direction based on the controller.                 |
| `TryGetObserver` |                                                             |
| `TryGetSubject`  |                                                             |

[← Previous Page](EntityController.md) | [Next Page →](Example%20Usage.md)
