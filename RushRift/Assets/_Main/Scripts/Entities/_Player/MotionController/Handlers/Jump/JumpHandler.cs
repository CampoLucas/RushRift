using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class JumpHandler : MotionHandler<JumpConfig>
    {
        private bool _readyToJump;
        private float _timer;
        
        public JumpHandler(JumpConfig config) : base(config)
        {
        }

        public override void OnUpdate(in MotionContext context, in float delta)
        {
            base.OnUpdate(in context, in delta);
            
            if (_readyToJump) return;

            if (_timer > 0)
            {
                _timer -= delta;
            }
            else
            {
                _readyToJump = true; // reset jump after cooldown
            }
        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            base.OnFixedUpdate(in context, in delta);
            
            if (context.Grounded && _readyToJump && context.Jump) Jump(context, delta);
        }

        private void Jump(in MotionContext context, in float delta)
        {
            _readyToJump = false;
            _timer = Config.Cooldown;
            
            context.Velocity = context.Velocity.XOZ(); // Clear vertical velocity before jump
            
            // Add jump forces
            context.AddForce(Vector3.up * (Config.Force * 1.5f), ForceMode.Impulse);
            context.AddForce(context.Normal * (Config.Force * 0.5f), ForceMode.Impulse);
        }
    }
}