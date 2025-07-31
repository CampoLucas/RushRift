using Game.Entities.Components.MotionController.Strategies;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class DashHandler : MotionHandler<DashConfig>
    {
        private bool _isDashing;
        private Vector3 _dashDir;
        private float _elapsed;
        
        // Collision Prevention
        private float _radius;
        private float _height;
        private float _halfHeight;

        private DashDirStrategyComposite _dirStrategy;
        private DashUpdateStrategyComposite _updateStrategy;
        private CompositeDashEndStrategy _endStrategy;
        
        public DashHandler(DashConfig config, DashDirStrategyComposite dirStrategy, DashUpdateStrategyComposite updateStrategy, CompositeDashEndStrategy endStrategy) : base(config)
        {
            _dirStrategy = dirStrategy;
            _updateStrategy = updateStrategy;
            _endStrategy = endStrategy;
        }

        public override void OnUpdate(in MotionContext context, in float delta)
        {
            if (!_isDashing && context.Dash) StartDash(context);

            if (_isDashing && _updateStrategy.OnDashUpdate(context, delta))
            {
                _isDashing = false;
            }
        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            base.OnFixedUpdate(in context, in delta);

            if (_isDashing && PerformDash(context, delta))
            {
                StopDash(context);
            }
        }

        private void StartDash(in MotionContext context)
        {
            _isDashing = true;
#if false
            _dashDir = context.Look.forward;
#else
            _dashDir = _dirStrategy.GetDir(context, Config);
            Debug.Log($"Dash dir is {_dashDir}");
#endif
            context.Velocity = Vector3.zero;
            
            _elapsed = 0;

            _radius = context.Collider.radius;
            _height = Mathf.Max(context.Collider.height, _radius * 2f);
            _halfHeight = (_height / 2f) - _radius;
            
            _updateStrategy.OnReset();
        }

        private bool PerformDash(in MotionContext context, in float delta)
        {
            if (!_isDashing)
            {
                return true;
            }
            
            if (_elapsed < Config.Duration)
            {
                var distance = Config.Force * delta;
                var up = context.Orientation.up;
                var center = context.Position + context.Collider.center;
                var point1 = center + up * _halfHeight;
                var point2 = center - up * _halfHeight;

                if (Physics.CapsuleCast(point1, point2, _radius, _dashDir, out var hit, distance))
                {
                    // Stop just before hitting object
                    context.Velocity = Vector3.zero;
                    context.MovePosition(context.Position + _dashDir * (hit.distance - 0.01f));

                    return true;
                }
                
                context.Velocity = _dashDir * Config.Force;
                
                _elapsed += Time.deltaTime;
                return false;
            }

            return true;
        }

        private void StopDash(in MotionContext context)
        {
            // Apply leftover momentum
            context.Velocity = _dashDir * (Config.Force * Config.MomentumMult);

            _endStrategy.OnDashEnd(context);
            
            _isDashing = false;
        }

        public override void OnDraw(Transform transform)
        {
            base.OnDraw(transform);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _dashDir * 2);
        }

        public override void Dispose()
        {
            base.Dispose();
            
            _dirStrategy?.Dispose();
            _dirStrategy = null;
            
            _updateStrategy?.Dispose();
            _updateStrategy = null;
            
            _endStrategy?.Dispose();
            _endStrategy = null;
        }
    }
}