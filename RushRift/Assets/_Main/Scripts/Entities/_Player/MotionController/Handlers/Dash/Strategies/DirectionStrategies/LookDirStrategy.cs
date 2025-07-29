using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class LookDirStrategy : IDashDirStrategy
    {
        public Vector3 GetDir(in MotionContext context)
        {
            return context.Look.forward.normalized;
        }
        
        public void Dispose()
        {
            
        }
    }
}