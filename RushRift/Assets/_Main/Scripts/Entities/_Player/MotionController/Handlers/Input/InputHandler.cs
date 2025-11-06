using Game.InputSystem;
using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    // Place holder class until the hole system is finished
    public class InputHandler : MotionHandler<InputConfig>
    {
        public InputHandler(InputConfig config) : base(config)
        {
            
        }

        public override void OnUpdate(in MotionContext context, in float delta)
        {
            context.MoveDirection = InputManager.GetValueVector(InputManager.MoveInput).XOZ();
            context.Jump = InputManager.OnButtonDown(InputManager.JumpInput);

            if (context.Jump)
            {
                context.JumpInputTime = Time.time;
            }
            //context.Dash = InputManager.GetActionPerformed(InputManager.SecondaryAttackInput);

            var lookRot = context.Look.localRotation.eulerAngles;
            var rot = context.Orientation.localRotation.eulerAngles;

            context.Orientation.localRotation = Quaternion.Euler(0, Mathf.Lerp(rot.y, lookRot.y, Config.Sens * Config.SensMult * delta), 0);
        }
    }
}