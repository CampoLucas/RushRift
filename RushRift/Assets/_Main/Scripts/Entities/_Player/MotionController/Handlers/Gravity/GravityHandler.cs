using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class GravityHandler : MotionHandler<GravityConfig>
    {
        private float _airTime;
        private bool _hovering;

        private Vector3 _pos;
        private Vector3 _groundPos;

        public GravityHandler(GravityConfig config) : base(config)
        {
        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            ApplyGravity(context, delta);
            //Hover(ref context, ref delta);
        }

        private void ApplyGravity(in MotionContext context, in float delta)
        {
            var pos = context.Position;

            _pos = pos;
            _groundPos = context.GroundPos;
            
            if (context.Grounded && !context.IsJumping)
            {
                _airTime = 0;

                var yDifference = pos.y - context.GroundPos.y;

                if (yDifference > Config.CorrDist)
                {
                    _hovering = false;
                    context.AddForce(Vector3.down * (Config.GndGrav * delta), ForceMode.Acceleration);
                }
                else
                {
                    if (!_hovering) // This stops all y velocity when falling, to do a bounce effect when falling, try decreasing the y velocity with a lerp
                    {
                        _hovering = true;

                        context.Velocity = context.Velocity.XOZ();
                    }
                    
                    Hover(context, delta);
                }
            }
            else
            {
                _hovering = false;
                _airTime += delta;

                float effectiveGravity;
                
                var end = Config.EndMult;
                if (context.IsJumping)
                {
                    // Gravity curve: start soft, then grow stronger
                    var start = Config.StartMult;
                    var gravityMultiplier = Mathf.Lerp(start, end, Mathf.Clamp01(_airTime / Config.CurveDur)); // 1 second to full gravity
                    effectiveGravity = Config.FallGrav * gravityMultiplier;
                }
                else
                {
                    effectiveGravity = Config.FallGrav * end;
                }

                context.AddForce(Vector3.down * (effectiveGravity * delta), ForceMode.Acceleration);
            }

            var velocity = context.Velocity;
            
            // Clamp excessive bounce when walking off edges
            if (!context.Jump && !context.Grounded && context.PrevGrounded) // !_jumping && !_grounded && _prevGrounded
            {
                var verticalSpeed = Vector3.Dot(velocity, Vector3.up);
                var groundAligned = Vector3.Dot(velocity, context.Normal);

                // If moving upward, and it's not aligned with the slope normal (i.e., likely a bounce)
                if (verticalSpeed > 0.5f && groundAligned < 0.2f)
                {
                    context.Velocity -= Vector3.up * (verticalSpeed * 0.5f);
                }

                // Optional: stronger gravity to pull player down
                context.AddForce(Vector3.down * (Config.FallGrav * 1.5f * delta), ForceMode.Acceleration);
            }

            // Clamp max fall speed
            if (!context.Grounded && velocity.y < -Config.MaxFallSpeed) // !_grounded
            {
                context.Velocity = new Vector3(velocity.x, -Config.MaxFallSpeed, velocity.z);
            }
        }

        private void Hover(in MotionContext context, in float delta)
        {
            var pos = context.Position;
            var targetY = context.GroundPos.y + Config.GndDist;

            if (context.Velocity.y < 0) context.Velocity = context.Velocity.XOZ();
#if false
            context.Position = new Vector3(pos.x, Mathf.Lerp(pos.y, targetY, Config.HoverSpeed * delta), pos.z);
#elif false
            context.Position = context.Position.XOZ(targetY);
#else
            context.MovePosition(context.Position.XOZ(targetY));
#endif
            //context.Velocity = context.Velocity.XOZ();

            // ToDo: test this, it is an alternative option
            //context.Position = pos.XOZ(Mathf.Lerp(pos.y, targetY, _config.HoverSpeed * Time.fixedDeltaTime));
        }

        public override void OnDraw(Transform transform)
        {
            base.OnDraw(transform);
            Gizmos.color = _hovering ? Color.magenta : Color.yellow;
            Gizmos.DrawSphere(_pos, .1f);
            Gizmos.DrawCube(_groundPos, Vector3.one * .1f);
        }
    }
}