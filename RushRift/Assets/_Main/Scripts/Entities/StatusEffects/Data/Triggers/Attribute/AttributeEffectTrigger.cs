using Game.Entities.Attribute;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public abstract class AttributeEffectTrigger : EffectTrigger
    {
        [Header("Attribute Type")]
        [SerializeField] private AttributeType attribute;
        
        protected bool TryGetAttribute(IController controller, out IAttribute atr)
        {
            switch (attribute)
            {
                case AttributeType.Health:
                    if (controller.GetModel().TryGetComponent<HealthComponent>(out var hc))
                    {
                        atr = hc;
                        return true;
                    }

                    atr = default;
                    return false;
                case AttributeType.Energy:
                    if (controller.GetModel().TryGetComponent<HealthComponent>(out var ec))
                    {
                        atr = ec;
                        return true;
                    }

                    atr = default;
                    return false;
                default:
                    atr = default;
                    return false;
            }
        }
    }
}