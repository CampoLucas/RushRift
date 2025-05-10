using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class CompareHealthTrigger : EffectTrigger
    {
        [SerializeField] private float value;
        [SerializeField] private Comparison comparison;
        
        public override Trigger GetTrigger(IController controller)
        {
            if (controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                var subject = healthComponent.OnValueChanged.Where(a => Compare(a.Item1));
                
                return new Trigger(subject, this);
            }

            return null;
        }

        public override bool Evaluate(ref IController controller)
        {
            if (controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                return Compare(healthComponent.Value);
            }

            return false;
        }

        private bool Compare(float health)
        {
            switch (comparison)
            {
                case Comparison.Equal:
                    return health == value;
                case Comparison.Different:
                    return health != value;
                case Comparison.Greater:
                    return health > value;
                case Comparison.GreaterOrEqual:
                    return health >= value;
                case Comparison.Less:
                    return health < value;
                case Comparison.LessOrEqual:
                    return health <= value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum Comparison
    {
        Equal,
        Different,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }
}