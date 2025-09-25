# ObserverComponent
**Type:** `class` | **Inherits from:** `MonoBehaviour` | **Implements:** [`IObserver`](IObserver.md)

### Description
Abstract `MonoBehaviour` class that implements the `IObserver<string>` interface. On the `OnNotify` receives messages from a `ISubject` and the sub class that implements it can interpret them.
It is used for when you want to assign a **observer** through the inspector.

### Abstract Methods
| Methods    | Description                                                              |
| ---------- | ------------------------------------------------------------------------ |
| `OnNotify` | Called by a `ISubject` has a string as a parameter, works like an event. |
### How To Implement It
If you have a `MonoBehaviour` class that you want to be notified by a **subject** via referencing in the inspector you should do this:

#### 1. Make your class **inherit from ObserverComponent:**
```csharp
// Inherit the ObserverComponent class
public class Door : ObserverComponent
{
	...
}
```

#### 2. Write the logic on the **OnNotify** method:
There are two ways to do it, having an dictionary with **Actions** or **IObservers**:
```csharp
// Implement logic to interpret the arguments using a dictionary.
// Using a dictionary, it does't compare strings.
public override void OnNotify(string arg)
{
	// Returns because it doesn't have an action for the given argument.
	if (!_myDictionary.TryGetValue(arg, out var result)) return; 
	
	// If it has an action, it invokes it.
	result.Invoke();
}
```

Another option is to compare the `string` inside the method:
```csharp
// Instead of using a dictionary you can directly compare the string, 
// but it is less recomended.
public override void OnNotify(string arg)
{
	if (arg == "Open")
	{
		// Open door.
	}
}
```


[← Previous Page](IObserver.md)