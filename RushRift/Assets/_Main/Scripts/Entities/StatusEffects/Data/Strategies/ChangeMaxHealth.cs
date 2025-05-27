using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class ChangeMaxHealth : EffectStrategy
    {
        [SerializeField] private float amount;

        public override void StartEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<HealthComponent>(out var health))
            {
                health.MaxValueModifier(amount);
            }
        }

        public override void StopEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<HealthComponent>(out var health))
            {
                health.MaxValueModifier(-amount);
            }
        }
    }
}