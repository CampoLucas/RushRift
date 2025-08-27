using Game.DesignPatterns.Observers;
using UnityEngine;

public abstract class ObserverComponent : MonoBehaviour, IObserver<string>
{
    public abstract void OnNotify(string arg);

    public virtual void Dispose()
    {
        
    }
}
