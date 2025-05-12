using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class MaxSpeed : EffectStrategy
    {
        [SerializeField] private float value = 1.5f;
        [SerializeField] private bool percentage = true;


        public override void StartEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<IMovement>(out var movement))
            {
                movement.AppendMaxSpeed(GetValue(movement.BaseMaxSpeed));
            }
        }

        private float GetValue(float maxSpeed)
        {
            return percentage ? maxSpeed * value / 100f : value;
        }

        public override void StopEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<IMovement>(out var movement))
            {
                movement.AppendMaxSpeed(-GetValue(movement.BaseMaxSpeed));
            }
        }
    }
}