# IModel
**Type:** `interface` | **Implemented in:** `Game.Entities`

### Description
Interface used to interact with the model.
### Methods
| Method                | Description                                                                                       |
| --------------------- | ------------------------------------------------------------------------------------------------- |
| `Init`                | Initialized the model with a reference to its controller                                          |
| `Update`              | Called every frame to update components.                                                          |
| `LateUpdate`          | Called after the Update to run late logic.                                                        |
| `FixedUpdate`         | Called on physics ticks to handle physics-related logic.                                          |
| `TryGetComponent`     | Attempts to retrieve a component of type `TComponent`. Returns `true` if found.                   |
| `TryAddComponent`     | Adds a component if it doesn’t already exist. Attaches its observers to the appropriate subjects. |
| `RemoveComponent`     | Removes a component and optionally disposes it. Detaches its observers from subjects.             |
| `HasComponents`       | Checks if the model contains a component of type `TComponent`.                                    |
| `OnDraw`              | Calls `OnDraw` for all components for editor debug rendering.                                     |
| `OnDrawSelected`      | Calls `OnDrawSelected` for all components when the entity is selected in the editor.              |
