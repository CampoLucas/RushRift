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
        
        
        public DashHandler(DashConfig config) : base(config)
        {
            // _height = Mathf.Max(Config.Height, Config.Radius * 2f);
            // _halfHeight = (_height / 2f) - Config.Radius;
        }

        public override void OnUpdate(in MotionContext context, in float delta)
        {
            if (!_isDashing && context.Dash) StartDash(context);
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
            
            context.Velocity = Vector3.zero;

#if true
            _dashDir = context.Look.forward;
#else
            var forward = context.Look.forward;
            var groundNormal = context.Normal;

            var dot = Vector3.Dot(forward, groundNormal);
            
            // If looking into the ground (dot > threshold), flatten the direction
            if (dot > 0.1f && context.Grounded)
            {
                _dashDir = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            }
            else
            {
                _dashDir = forward.normalized;
            }
#endif
            
            
            _elapsed = 0;

            _radius = context.Collider.radius;
            _height = Mathf.Max(context.Collider.height, _radius * 2f);
            _halfHeight = (_height / 2f) - _radius;
        }

        private bool PerformDash(in MotionContext context, in float delta)
        {
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

                    Debug.Log("DashTest: Returning");
                    
                    return true;
                }
                
                Debug.Log("DashTest: Moving");
                

#if true
                context.Velocity = _dashDir * Config.Force;
#else
                Vector3 dashDir = _dashDir;

                if (context.Grounded)
                {
                    float dot = Vector3.Dot(dashDir, context.Normal);

                    if (dot > 0.1f)
                    {
                        dashDir = Vector3.ProjectOnPlane(dashDir, context.Normal).normalized;
                    }
                }

                context.Velocity = dashDir * Config.Force;
#endif
                

                _elapsed += Time.deltaTime;
                return false;
            }

            return true;
        }

        private void StopDash(in MotionContext context)
        {
            // Apply leftover momentum
            context.Velocity = _dashDir * (Config.Force * Config.MomentumMult);

            _isDashing = false;
        }

        public override void OnDraw(Transform transform)
        {
            base.OnDraw(transform);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _dashDir * 2);
        }
    }
}