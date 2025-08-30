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
- It's recommended to create a class that inherits from this `ScriptableObject` and add the components in the `Init` method rather than subclassing [EntityModel](EntityModel.md) directly.
- See the upcoming example page for creating a concreate `EnemyModelSO` to understand how to instantiate and configure components in practice.

<div style="display: flex; justify-content: space-between;">
  <a href="EntityModel.md">← Previous Page</a>
  <a href="ExampleUsage.md">Next Page →</a>
</div>
