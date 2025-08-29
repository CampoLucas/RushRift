using Game.Entities.Components.MotionController.Strategies;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class DashHandler : MotionHandler<DashConfig>
    {
        public DashDirStrategyComposite DirStrategy { get; private set; }
        public DashUpdateStrategyComposite UpdateStrategy { get; private set; }
        public CompositeDashEndStrategy EndStrategy { get; private set; }
        
        private bool _isDashing;
        private Vector3 _dashDir;
        private float _elapsed;
        private float _elapsedCooldown;
        
        // Collision Prevention
        private float _radius;
        private float _height;
        private float _halfHeight;
        
        public DashHandler(DashConfig config, DashDirStrategyComposite dirStrategy, DashUpdateStrategyComposite updateStrategy, CompositeDashEndStrategy endStrategy) : base(config)
        {
            DirStrategy = dirStrategy;
            UpdateStrategy = updateStrategy;
            EndStrategy = endStrategy;
        }

        public override void OnUpdate(in MotionContext context, in float delta)
        {
            base.OnUpdate(in context, in delta);

            if (!_isDashing)
            {
                if (_elapsedCooldown > 0)
                {
                    _elapsedCooldown -= delta;
                }
                else if (context.Dash)
                {
                    _elapsedCooldown = Config.Cooldown;
                    StartDash(context);
                }
            }
            
            
            if (!_isDashing && _elapsedCooldown <= 0 && context.Dash) StartDash(context);

            if (_isDashing && UpdateStrategy.OnUpdate(context, delta))
            {
                //_elapsedCooldown = Config.Cooldown;
                _isDashing = false;
            }
            
            // Reset the dash input
            context.Dash = false;
        }

        public override void OnLateUpdate(in MotionContext context, in float delta)
        {
            base.OnLateUpdate(in context, in delta);

        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            base.OnFixedUpdate(in context, in delta);

            if (_isDashing && PerformDash(context, delta))
            {
                //_elapsedCooldown = Config.Cooldown;
                StopDash(context);
            }
        }

        private void StartDash(in MotionContext context)
        {
            _isDashing = true;
#if false
            _dashDir = context.Look.forward;
#else
            _dashDir = DirStrategy.GetDir(context, Config);
#endif
            context.Velocity = Vector3.zero;
            
            _elapsed = 0;

            _radius = context.Collider.radius;
            _height = Mathf.Max(context.Collider.height, _radius * 2f);
            _halfHeight = (_height / 2f) - _radius;
            
            UpdateStrategy.OnReset();
        }

        private bool PerformDash(in MotionContext context, in float delta)
        {
            if (!_isDashing)
            {
                return true;
            }

            if (UpdateStrategy.OnLateUpdate(context, delta))
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

                if (Physics.CapsuleCast(point1, point2, _radius, _dashDir, out var hit, distance) && UpdateStrategy.OnCollision(context, hit.collider)) // Call the on collision from the strategies
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

            EndStrategy.OnDashEnd(context);
            
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
            
            DirStrategy?.Dispose();
            DirStrategy = null;
            
            UpdateStrategy?.Dispose();
            UpdateStrategy = null;
            
            EndStrategy?.Dispose();
            EndStrategy = null;
        }

        public float GetCost() => Config.Cost;
        public bool IsDashing() => _isDashing;
        public bool CanDash(IController controller)
        {
            if (_isDashing || _elapsedCooldown > 0)
            {
                return false;
            }
            
            if (controller.GetModel().TryGetComponent<EnergyComponent>(out var energy))
            {
                return energy.Value >= Config.Cost;
            }

            return false;
        }
    }
}