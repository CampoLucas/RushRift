# EntityView
**Type:** `class` | **Inherits from:** `MonoBehaviour` | **Implements:** [`IView`](IView.md) | **Implemented in:** `Game.Entities`

### Description
Generic view class that handles animation logic and VFXs for an entity. Designed to work with the entity model system by implementing [`IView`](IView.md).
It's best to communicate between components and the view using the **Observer pattern:**
- Components expose **Subjects.**
- The view gets the component reference through the `IController` and attaches **Observers** to those subjects. This avoids coupling and works like data-driven signaling in ECS-style architectures.

### Public Methods
| Methods | Description                                      |
| ------- | ------------------------------------------------ |
| `Init`  | Initializes the view with an array of Animators. |
| `Play`  | Plays the specified animation on all animators.  |


[← Previous Page](../Model/Example%20Usage.md) | [→ Next Page](IView.md)

