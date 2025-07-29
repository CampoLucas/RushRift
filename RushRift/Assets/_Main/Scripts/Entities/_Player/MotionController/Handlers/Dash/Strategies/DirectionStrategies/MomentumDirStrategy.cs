using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class MomentumDirStrategy : IDashDirStrategy
    {
        public Vector3 GetDir(in MotionContext context)
        {
            return context.Velocity.normalized;
        }
        
        public void Dispose()
        {
            
        }
    }
}