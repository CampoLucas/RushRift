using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashLookStrategy : DashDirStrategy<DashDirConfig>
    {
        public DashLookStrategy(DashDirConfig config) : base(config) { }
        
        protected override Vector3 OnGetDir(in MotionContext context, in DashConfig config)
        {
            return context.Look.forward.normalized;
        }
    }
}