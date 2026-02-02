using System;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.AttackSystem.Attributes
{
    public class AttributePredicate : ComboPredicate
    {
        public enum Attribute
        {
            HealthComponent, EnergyComponent
        }

        [Header("Attribute")]
        [SerializeField] private Attribute attribute;

        public bool TryGetAttribute(IController controller, out IAttribute atr)
        {
            switch (attribute)
            {
                case Attribute.HealthComponent:
                    if (controller.GetModel().TryGetComponent<HealthComponent>(out var health))
                    {
                        atr = health;
                        return true;
                    }

                    atr = null;
                    return false;
                case Attribute.EnergyComponent:
                    if (controller.GetModel().TryGetComponent<EnergyComponent>(out var energy))
                    {
                        atr = energy;
                        return true;
                    }

                    atr = null;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}