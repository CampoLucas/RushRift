# View Example
The **EntityView** is a `MonoBehaviour` responsable only for **visual logic** (VFX, animations, etc.).
It does not change or drive the entity's behavior, the controller and model must always work, evin if they don't have any view.

The ideal way to implement a view is to **attach observers to subjects from the entity's components**. This keeps the view decoupled from the gameplay logic, while still reacting to events or value changes.

### Example
Below there is a simplified example of a `LaserView` that listens to subjects from the `LaserComponent` and `HealthComponent`.
```csharp
public class LaserView : EntityView
{
	[SerializeField] private ParticleSystem destroyVfx;
	
	private IController _controller;
	private ISubject _onDestroy;
	private ActionObserver _destroyObserver;
	
	private void Awake()
	{
		_controller = GetComponent<IController>();
		
		_destroyObserver = new ActionObserver(DestroyVFXHandler);
	}
	
	// Attach the observers if the model has the needed component
	private void Start()
    {
        var model = _controller.GetModel();

        if (model.TryGetComponent<HealthComponent>(out var health))
        {
	        // save the subject reference to detach the observer later
            _onDestroy = health.OnEmptyValue;
            _onDestroy.Attach(_destroyObserver);
        }
    }
    
    // Do the view logic needed
    private void DestroyVFXHandler()
    {
	    destroyVFX.Play();
    }
    
    // Detach the observer when disposint the view, for safety reasons
    private void OnDispose()
    {
	    _onDestroy?.Detach(_destroyObserver);
    }
}
```

#### Why this is the recommended way
- Views remain **completely optional**.
- The gameplay logic (controller + model) doesn’t depend on whether a view is present.
- Observers make the view purely **reactive**, it only updates visuals when notified by the model’s components.

#### Alternative: Getting the View from the Controller
It’s also possible to access the view directly through the controller:

```csharp
if (controller.GetView() is LaserView laserView) 
{
	laserView.DoSomething();
}
```

> [!WARNING]
> **This is less recommended** because:
> - It **couples logic to a specific view implementation** (breaking the “views are optional” principle).
> - It makes systems depend on visuals, which should never happen in a clean separation of responsibilities.

## More Examples
- [Controller Example](../Controller/Example%20Usage.md)
- [Model Example](../Model/Example%20Usage.md)
- Component Example
- 
[← Previous Page](IView.md)