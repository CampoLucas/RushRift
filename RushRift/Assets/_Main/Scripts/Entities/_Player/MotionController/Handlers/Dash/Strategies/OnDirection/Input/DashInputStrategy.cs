using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashInputStrategy : DashDirStrategy<DashDirConfig>
    {
        public DashInputStrategy(DashDirConfig config) : base(config)
        {
        }
        
        protected override Vector3 OnGetDir(in MotionContext context, in DashConfig config)
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
    }
}