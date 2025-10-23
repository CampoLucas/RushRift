using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public interface IEntityComponent : System.IDisposable
    {
        NullCheck<ActionObserver<bool>> OnLoading { get; }
        bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer);
        bool TryGetLateUpdate(out DesignPatterns.Observers.IObserver<float> observer);
        bool TryGetFixedUpdate(out DesignPatterns.Observers.IObserver<float> observer);
        
        void OnDraw(Transform origin);
        void OnDrawSelected(Transform origin);
    }
}