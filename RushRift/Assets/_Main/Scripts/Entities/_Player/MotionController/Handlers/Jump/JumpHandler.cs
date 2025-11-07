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

            context.IsJumping = !_readyToJump;
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

            var jumpRequested = context.Jump;
            var grounded = context.Grounded;
            
            // Jump buffer
            if (grounded && !jumpRequested)
            {
                jumpRequested = (Time.time - context.JumpInputTime) <= Config.JumpBufferTime;
            }
            
            // Coyote time
            var canCoyoteJump = !grounded && (Time.time - context.StopGroundedTime) <= Config.CoyoteTime;
            
            if ((grounded || canCoyoteJump) && _readyToJump && jumpRequested)
            {
                Jump(context, delta);
            }
        }

        private void Jump(in MotionContext context, in float delta)
        {
            _readyToJump = false;
            _timer = Config.Cooldown;
            context.LastJumpTime = Time.time;
            
            AudioManager.Play("Jump");

            var velocity = context.Velocity;
            
            //context.Velocity = Vector3.zero;
            context.Velocity = context.Velocity.XOZ(); // Clear vertical velocity before jump
            
            // Add jump forces
            // var inputDir = Config.InputInfluence > 0 ? Camera.main ?
            //     Camera.main.transform.TransformDirection(context.MoveDirection) * Config.InputInfluence : 
            //     Vector3.zero : 
            //     Vector3.zero;
            
            var inputDir = Config.InputInfluence > 0 ? 
                context.Orientation.TransformDirection(context.MoveDirection) * Config.InputInfluence : 
                Vector3.zero;

            var h = velocity.XOZ();

            var velocityDir = Config.VelocityInfluence > 0 ? h.normalized * Config.InputInfluence : Vector3.zero;
            if (h.magnitude < Config.MinHorVelocity)
            {
                velocityDir = Vector3.zero;
            }
            
            var upDir = Config.UpInfluence > 0 ? Vector3.up * Config.UpInfluence : Vector3.zero;
            var normalDir = Config.NormalInfluence > 0
                ? Vector3.ProjectOnPlane(Vector3.up, context.Normal) * Config.NormalInfluence
                : Vector3.zero;

            var force = false
                ? Config.Force * (velocity.magnitude * Config.VelocityInfluence)
                : Config.Force;
            
            context.AddForce((upDir + inputDir + velocityDir + normalDir).normalized * force, ForceMode.Impulse);
            //context.AddForce(context.Normal * (Config.Force * 0.5f), ForceMode.Impulse);
        }
    }
}