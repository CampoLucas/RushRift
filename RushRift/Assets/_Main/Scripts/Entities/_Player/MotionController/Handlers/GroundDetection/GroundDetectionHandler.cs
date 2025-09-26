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

            context.PrevGrounded = context.Grounded;
            if (Physics.SphereCast(sphereOrigin, Config.Radius, Vector3.down, out var hit, Config.Distance,
                    Config.Layer, QueryTriggerInteraction.Ignore))
            {
                context.Grounded = true;
                context.Normal = hit.normal;
                context.GroundPos = hit.point;
                context.GroundAngle = Vector3.Angle(context.Normal, Vector3.up);

                if (!context.PrevGrounded && context.Grounded) // Just landed
                {
                    // ToDo: this in slippery handler
                    
                    // if (_lastHorizontalSpeed > fallSlideTriggerSpeed)
                    // {
                    //     // Map speed over threshold to slide amount (clamped)
                    //     var slideAmount = Mathf.Clamp01((_lastHorizontalSpeed - fallSlideTriggerSpeed) /
                    //                                     (movementData.MaxSpeed - fallSlideTriggerSpeed));
                    //     //_slippery = Mathf.Max(_slippery, slideAmount * fallMaxSlideAmount);
                    //     _slippery += Mathf.Clamp(slideAmount, 0, fallMaxSlideAmount);
                    // }
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
                        return;
                    }
                }
                
                context.Grounded = false;
                context.Normal = Vector3.up;
                context.GroundAngle = 0;
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