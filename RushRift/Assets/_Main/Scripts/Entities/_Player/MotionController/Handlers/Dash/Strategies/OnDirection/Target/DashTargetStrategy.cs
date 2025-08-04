using Game.Detection;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashTargetStrategy : DashDirStrategy<DashTargetConfig>
    {
        private Collider[] _colliders;
        private IPredicate<FOVParams> _fov;
        
        public DashTargetStrategy(DashTargetConfig config) : base(config)
        {
            _colliders = new Collider[config.MaxColliders];
            _fov = config.FovBuilder.GetFOV();
        }
        
        protected override Vector3 OnGetDir(in MotionContext context, in DashConfig config)
        {
            var pos = context.Position;
            var overlaps = Physics.OverlapSphereNonAlloc(pos, Config.Range, _colliders, Config.Layer);
            if (overlaps > 0)
            {
                for (var i = 0; i < overlaps; i++)
                {
                    var other = _colliders[i];
                    if (!other) continue;

                    var targetPos = other.transform.position;
                    var fovParams = FOVParams.GetFOVParams(pos, context.Look.forward, targetPos);
                    
                    if (_fov.Evaluate(ref fovParams))
                    {
                        return (targetPos - pos).normalized;
                    }
                }
            }
            
            return Vector3.zero;
        }

        protected override void OnDispose()
        {
            _colliders = null;
            _fov?.Dispose();

            _fov = null;
        }
    }
}