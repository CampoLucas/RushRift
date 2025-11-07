using Game.DesignPatterns.Observers;
using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class GroundDetectionHandler : MotionHandler<GroundDetectionConfig>
    {
        
        
        // Gizmos
        private bool _groundedGizmos;
        private Vector3 _normalGizmos;

        public GroundDetectionHandler(GroundDetectionConfig config) : base(config)
        {
            
        }
        
        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            var pos = context.Origin.position;
            var sphereOrigin = pos + (Vector3.up * Config.Offset);
            
            var vel = context.Velocity;
            var horVel = vel.XOZ();

            var wasGrounded = context.Grounded;
            context.PrevGrounded = wasGrounded;
            
            if (Physics.SphereCast(sphereOrigin, Config.Radius, Vector3.down, out var hit, Config.Distance,
                    Config.Layer, QueryTriggerInteraction.Ignore))
            {
                context.Grounded = true;
                context.Normal = hit.normal;
                context.GroundPos = hit.point;
                context.GroundAngle = Vector3.Angle(context.Normal, Vector3.up);

                if (!wasGrounded && context.Grounded)
                {
                    context.StopGroundedTime = -1;
                }
            }
            else
            {
                // Ground snapping check
                // Only do it if falling slightly and not jumping
                if (!context.Jump && vel.y <= 2f)
                {
                    var snapOrigin = pos + Vector3.up * 0.5f;
                    if (Physics.Raycast(snapOrigin, Vector3.down, out var snapHit, 1f, Config.Layer))
                    {
                        context.Grounded = true;
                        context.Normal = snapHit.normal;
                        context.GroundAngle = Vector3.Angle(context.Normal, Vector3.up);

                        if (!wasGrounded && context.Grounded)
                            context.StopGroundedTime = -1f;
                        
                        return;
                    }
                }

                context.Grounded = false;
                context.Normal = Vector3.up;
                context.GroundAngle = 0;
                
                if (wasGrounded && !context.Grounded)
                {
                    context.StopGroundedTime = Time.time;
                }
            }

#if UNITY_EDITOR
            _groundedGizmos = context.Grounded;
            _normalGizmos = context.Normal;
#endif
        }

        public override void OnDraw(Transform transform)
        {
            var pos = transform.position;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, _normalGizmos);
            
            var sphereOrigin = pos + (Vector3.up * Config.Offset);
            Gizmos.color = _groundedGizmos ? Color.green : new Color(0, .5f, 0);
            Gizmos.DrawWireSphere(sphereOrigin, Config.Radius);

            Gizmos.color = _groundedGizmos ? Color.red : new Color(.5f, 0, 0);
            Gizmos.DrawWireSphere(sphereOrigin + (Vector3.down * Config.Distance), Config.Radius);
        }
    }
}