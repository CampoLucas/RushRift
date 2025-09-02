# Controller Example
This example demonstrates how to make a custom `EntityController` in Unity. It shows how to:
- Initialize a controller with a runtime `EntityModel` from a `EntityModelSO` and a `EntityView`.
- Access entity components via the controller.
- Keep the entity logic flexible by allowing dynamic addition or removal of components without modifying the controller.

### Setup
To use the system, you need three main classes: a `EntityController`, an `EntityModelSO` and an `EntityView`.
- For a **simple entity**, you can use the base `EntityController` and `EntityView` classes directly, no subclassing is required.
- If your entity needs **custom behavior**, create subclasses of the `EntityController` and/or `EntityView` for it.
- For the **model**, you must create a class that inherits from `EntityModelSO` to add components when the model is initialized.
The `EntityController` should be assigned to the scene or prefab `GameObject` and must reference the corresponding `EntityModelSO`. This setup ensures the controller can initialize the runtime model proxy and manage its components.

## Simple Entity
### Overview
This example shows how to set up a basic entity using the system. It highlights the minimal steps needed to have a working entity with a **model**, **controller** and **view** without delving into custom behavior or components.

### 1. Required Classes
To create a functional entity, you need:
- **EntityControler:** Assign to your **GameObject** or **prefab**. Handles runtime logic and interaction between model and view.
- **EntityModelSO:** `ScriptableObject` that defines which components the entity has. Subclass this to add components.
- **EntityView:** Handles the visuals of the entity. You can use the base class if you only need the entity to have animation.

### 2. Setup in Scene or Prefab
1. Add an `EntityController` component to your `GameObject`.
2. Assign a reference to the `EntityModelSO`.
3. Add an `EntityView` component to you `GameObject`.

### 3. Runtime Behavior
- The controller creates a model proxy from the `ScriptableObject`.
- Components are added dynamically to the model at runtime.
- Observers in the model ensure that only components with `Update`, `LateUpdate` or `FixedUpdate` logic run, keeping performance efficient.

## Custom Entity
To make a custom entity you will have to inherit the `EntityController` class. Inside the class you can make the following things:

### 1. Attaching Observers to Subjects from Components
This show how a custom controller can directly link one component's subject to another component's observer or an observer from the controller.

**Attaching a component's observer to another component's subject**
```csharp
public class LaserController : EntityController
{
	// Override the start method, so its called after the model is initialized
	protected override void Start()
	{
		base.Start();
		
		var model = GetModel(); // Gets the controller's model proxy.
		
		// First we check if it has both components
		if (model.TryGetComponent<HealthComponent>(out var healthComponent) && 
			model.TryGetComponent<DestroyComponent>(out var destroyComponent))
		{
			// Attach the destroy entity observer to the on zero health subject.
			healthComponent.OnZeroHealthSubject
				.Attach(destroyComponent.DestroyEntityObserver);
		}
	}
}
```

**Attaching observers from the controller to a component's subject**
```csharp
public class LaserComponent : EntityController
{
	// Save the reference of the observer to dispose later.
	private ActionObserver _onDieObserver; 
	
	// Override the awake to save the reference of the on die observer
	protected override void Awake()
	{
		base.Awake();

		_onDieObserver = new ActionObserver(OnDieHandler);
	}

	// Override the start method, so its called after the model is initialized
	protected override void Start()
	{
		base.Start();
		
		var model = GetModel(); // Gets the controller's model proxy.
		
		// First we check if it has the health component
		if (model.TryGetComponent<HealthComponent>(out var healthComponent))
		{
			// Attach the on die observer to the on zero health subject.
			healthComponent.OnZeroHealthSubject.Attach(_onDieObserver);
		}
	}
	
	// Dispose the observer to clean memory
	protected override void OnDispose()
	{
		base.OnDispose();

		_onDieObserver?.Dispose();
		_onDieObserver = null;
	}
	
	private void OnDieHandler()
	{
		// Custom logic.
	}
}
```

**Key points:**
- You can get the components on the `Start()` method.
- Use the `Attach(observer)`, from the subject, to link them.
- Keeps the wiring logic in the controller, not hidden inside components.

### 2. Setting the Observers and Subjects of the Entity
This allows other systems to attach observers dynamically, without hardcoding dependencies.

```csharp
public class LaserController : EntityController
{
	protected override void Awake()
	{
		AddObserver("on", new ActionObserver(OnHandler));
		AddObserver("off", new ActionObserver(OffHandler));
	}
	
	 private void OnHandler()
	{
		// Execute logic to turn on.
	}

	private void OffHandler()
	{
		// Execute logic to turn on.
	}
}
```

Then a component or another class you could get the observer to attach it to a `ISubject`. Like this:
```csharp
public class Button : MonoBehaviour
{
	private ISubject _onSubject;

	private void AttachController(IController controller)
	{
		if (controller.TryGetObserver("on", out var onObserver))
		{
			_onSubject.Attach(onObserver);
		}
	}
}

```

The main purpose of this is for entities to communicate between other entities and level elements, that way creating creative level designs.

**Key Points:**
- `AddSubject(key, subject)` registers a subject.
- `AddObserver(key, observer)` registers an observer.
- Lets entities interact with other entities and level elements without hardcoding.
- Useful for creative level designs.

### 3. How to get the components
To access a controller’s components, you first need a reference to the controller. This can come from a method parameter, a serialized field, or a **GameObject** lookup.

#### As a parameter on a method:
```csharp
private void MyMethod(IController controller)
{
	// Check if the controller is not null and check if it has the component
	if (controller != null &&
		controller.GetModel().TryGetComponent<ExampleComponent>(out var comp))
	{
		// Do something.
	}
}
```

#### From a serialized controller on a `MonoBehaviour`:
```csharp
public class ExampleClass : MonoBehaviour
{
	[SerializeField] private PlayerController player;
	
	public void Start()
	{
		// Check if the player controller is not null 
		// and check if it has the component
		if (player != null &&
			player.GetModel().TryGetComponent<ExampleComponent>(out var comp))
		{
			// Do something.
		}
	}
} 
```

#### From a `GameObject`:
```csharp
private void MyMethod(GameObject gObject)
{
	// First we get the IController
	// If it doesn't have a controller returns
	if (!gObject.TryGetComponent<IController>(out var controller)) return;

	// Check if the controller is not null and check if it has the component
	if (controller != null &&
		controller.GetModel().TryGetComponent<ExampleComponent>(out var comp))
	{
		// Do something.
	}
}
```

### 4. How to check if it has a component
With these system we can **agnostically** check if an entity has a component and change the behavior. The best case example is on a **BehaviourTree**, where we check if it has a component to do a specific behavior and another if it doesn't.
```csharp
if (controller.GetModel().HasComponent<ExampleComponent>())
{
	return NodeState.Success;
}
else
{
	return NodeState.Failure;
}
```

Since components can be added on runtime, entities could have components to change how other systems interact with them.

## More Examples
- [Model Example](../Model/Example%20Usage.md)
- [View Example](../View/Example%20Usage.md)
- Component Example

[← Previous Page](IController.md) | [Next Page →](../Model/EntityModel.md)
