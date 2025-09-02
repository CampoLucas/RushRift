# IView
**Type:** `interface` | **Implements:** [`IView`](IView.md) | **Implemented in:** `Game.Entities`

### Description
Interface used by other scripts or controllers to interact with a view. Provides methods for initializing visuals and controlling animations. 
Concrete `EntityView` implementations attach observers to specific components via the controller to update visuals in response to data changes.

### Methods
| Method | Description                                                          |
| ------ | -------------------------------------------------------------------- |
| `Init` | Initializes the view with the required references (e.g., Animators). |
| `Play` | Plays the specified animation on all animators.                      |


[← Previous Page](EntityView.md) | [→ Next Page](Example%20Usage.md)