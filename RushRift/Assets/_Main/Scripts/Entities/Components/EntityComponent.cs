using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class EntityComponent : IEntityComponent
    {
        public NullCheck<ActionObserver<bool>> OnLoading { get; protected set; }
        public virtual bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public virtual bool TryGetLateUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public virtual bool TryGetFixedUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public virtual void OnDraw(Transform origin) { }
        public virtual void OnDrawSelected(Transform origin) { }
        
        public void Dispose()
        {
            OnDispose();
            OnLoading.Dispose();
        }
        
        protected virtual void OnDispose() {}
    }
}