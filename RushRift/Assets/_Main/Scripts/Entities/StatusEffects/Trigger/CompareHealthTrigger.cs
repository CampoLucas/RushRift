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
        
        public override ISubject GetSubject(IController controller)
        {
            if (controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                Debug.Log("Return subject");
                return healthComponent.OnValueChanged.Where(a => Compare(a.Item1));
            }

            Debug.Log("Return null");
            return null;
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