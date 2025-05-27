using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Attribute
{
    public abstract class AttributeEffectStrategy : EffectStrategy
    {
        [Header("Attribute Type")]
        [SerializeField] private AttributeType attribute;
        
        [Header("Amount")]
        [SerializeField] private bool percentage = true;
        [SerializeField] private float amount = 30;
        
        public sealed override void StartEffect(IController controller)
        {
            Debug.Log("SuperTest: Start Attribute Effect Strategy");
            if (TryGetAttribute(controller, out var atr))
            {
                OnStartEffect(atr);
            }
            else
            {
                Debug.Log("SuperTest: no effect");
            }
        }

        public sealed override void StopEffect(IController controller)
        {
            if (TryGetAttribute(controller, out var atr))
            {
                OnStopEffect(atr);
            }
        }
        
        protected abstract void OnStartEffect(IAttribute atr);
        protected abstract void OnStopEffect(IAttribute atr);
        
        protected float GetAmount(float maxValue)
        {
            return percentage ? maxValue * amount / 100f : amount;
        }

        private bool TryGetAttribute(IController controller, out IAttribute atr)
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
                    if (controller.GetModel().TryGetComponent<EnergyComponent>(out var ec))
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