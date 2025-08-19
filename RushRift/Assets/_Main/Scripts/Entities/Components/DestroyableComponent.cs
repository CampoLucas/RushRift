using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

public class DestroyableComponent : IEntityComponent
{
    private IObserver _destroyObserver;

    public DestroyableComponent(IObserver destroyObserver)
    {
        _destroyObserver = destroyObserver;
    }
    
    public void DestroyEntity()
    {
        if (_destroyObserver != null) _destroyObserver.OnNotify();
    }
    
    public bool TryGetUpdate(out IObserver<float> observer)
    {
        observer = null;
        return false;
    }

    public bool TryGetLateUpdate(out IObserver<float> observer)
    {
        observer = null;
        return false;
    }

    public bool TryGetFixedUpdate(out IObserver<float> observer)
    {
        observer = null;
        return false;
    }
    
    public void Dispose()
    {
        
    }

    public void OnDraw(Transform origin) { }
    public void OnDrawSelected(Transform origin) { }
}
