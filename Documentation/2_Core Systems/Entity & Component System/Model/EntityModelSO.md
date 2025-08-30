# EntityModelSO
**Type:** `class` | **Inherits from:** [SerializableSO](/Documentation/3_Tools/SerializableSO.md) | **Implemented in:** `Game.Entities`
### Description
Abstract base `ScriptableObject` class for creating runtime Model proxies.
### Abstract Methods
| Method     | Description                                         |
| ---------- | --------------------------------------------------- |
| `GetProxy` | Creates a proxy instance to be used during runtime. |
| `Init`     | Initializes the model components.                   |
### Notes
- Its better to make a class that inherits from this class and add the components in the `Init` method instead of making a class that inherits from [EntityModel](EntityModel.md).
