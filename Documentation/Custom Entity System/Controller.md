# Controller
`EntityController`
**Type:** `ObserverComponent`
**Purpose:** Main runtime entry point for an entity. Initializes the model and executes the components.
**Key Features:**
- Hold references to `Origin`, `Joints`, `EntityModelSO` and `Animators`
- Manages a finite state machine (`EntityStateMachine`)
- Forwards Unity's `Update`, `LateUpdate` and `FixedUpdate` calls to the model.
- Has `Observer` and `Subject` functionality.
- Implements `IDisposable` to clean up proxies and FSM.

`IController`