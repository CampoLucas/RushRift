using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;

public sealed class DestroyableComponent : EntityComponent
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
    
    protected override void OnDispose()
    {
        _destroyObserver.Dispose();
        _destroyObserver = null;
    }
}
