using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashMomentumStrategy : DashDirStrategy<DashDirConfig>
    {
        public DashMomentumStrategy(DashDirConfig config) : base(config)
        {
        }
        
        protected override Vector3 OnGetDir(in MotionContext context, in DashConfig config)
        {
            return context.Velocity.normalized;
        }
    }
}