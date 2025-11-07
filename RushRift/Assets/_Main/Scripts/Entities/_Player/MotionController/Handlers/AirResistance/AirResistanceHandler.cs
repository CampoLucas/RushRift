using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class AirResistanceHandler : MotionHandler<AirResistanceConfig>
    {
        public AirResistanceHandler(AirResistanceConfig config) : base(config)
        {
        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            if (context.Grounded) return;

            var velocity = context.Velocity;
            var horVelocity = velocity.XOZ();
            var dampenedVel = horVelocity * Config.AirResistance;

            context.Velocity = dampenedVel.XOZ(velocity.y);
        }
    }
}