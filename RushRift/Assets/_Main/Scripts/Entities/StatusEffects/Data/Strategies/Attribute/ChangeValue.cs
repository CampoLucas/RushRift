using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Attribute
{
    public class ChangeValue : AttributeEffectStrategy
    {
        protected override void OnStartEffect(IAttribute atr)
        {
            var a = GetAmount(atr.StartMaxValue);
            if (a > 0)
            {
                atr.Increase(a);
            }
            else
            {
                atr.Decrease(a);
            }
        }

        protected override void OnStopEffect(IAttribute atr)
        {
            
        }
    }
}