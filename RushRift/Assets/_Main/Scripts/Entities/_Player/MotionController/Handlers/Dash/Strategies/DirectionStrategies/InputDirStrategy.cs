using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class InputDirStrategy : IDashDirStrategy
    {
        public Vector3 GetDir(in MotionContext context)
        {
            var moveDir = context.MoveDirection;
            
            var forward = context.Look.forward.XOZ(true);
            var right = context.Look.right.XOZ(true);
            
            if (moveDir == Vector3.zero)
            {
                return forward;
            }

            return (forward * moveDir.z + right * moveDir.x).normalized;

        }
        
        public void Dispose()
        {
            
        }
    }
}