using Game.Entities.Components;

namespace Game.Entities.Attribute
{
    public class ChangeRegenRate : AttributeEffectStrategy
    {
        protected override void OnStartEffect(IAttribute atr)
        {
            atr.RegenRateModifier(GetAmount(atr.StartRegenRate));
        }

        protected override void OnStopEffect(IAttribute atr)
        {
            atr.RegenRateModifier(-GetAmount(atr.StartRegenRate));
        }
    }
}