using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class GravityHandler : MotionHandler<GravityConfig>
    {
        private float _airTime;
        private bool _hovering;

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
            
            if (context.Grounded && !context.Jump)
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
            
                // Gravity curve: start soft, then grow stronger
                var start = Config.StartMult;
                var end = Config.EndMult;
                var gravityMultiplier = Mathf.Lerp(start, end, Mathf.Clamp01(_airTime / Config.CurveDur)); // 1 second to full gravity
                var effectiveGravity = Config.FallGrav * gravityMultiplier;

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

            context.Position = new Vector3(pos.x, Mathf.Lerp(pos.y, targetY, Config.HoverSpeed * delta),
                pos.z);

            // ToDo: test this, it is an alternative option
            //context.Position = pos.XOZ(Mathf.Lerp(pos.y, targetY, _config.HoverSpeed * Time.fixedDeltaTime));
        }
    }
}