using System;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public abstract class BaseMotionHandler : IDisposable
    {
        public virtual HandlerUpdateType UpdateType => HandlerUpdateType.None; // To use multiple updates: HandlerUpdateType.Update | HandlerUpdateType.FixedUpdate
        public abstract int Order();

        public virtual void OnUpdate(in MotionContext context, in float delta) { }
        public virtual void OnLateUpdate(in MotionContext context, in float delta) { }
        public virtual void OnFixedUpdate(in MotionContext context, in float delta) { }
        
        public bool HasUpdateType(HandlerUpdateType type)
        {
            if (UpdateType == HandlerUpdateType.None) // No updates at all
                return false;

            return (UpdateType & type) != 0; // True if the flag is present
        }
        
        public virtual void Dispose() { }
        
        // Gizmos
        public virtual void OnDraw(Transform transform) { }
        public virtual void OnDrawSelected(Transform transform) { }
    }
    
    [Flags]
    public enum HandlerUpdateType {
        None = 0,
        Update = 1 << 0,      // 1
        FixedUpdate = 1 << 1, // 2
        LateUpdate = 1 << 2,  // 4
    }
}