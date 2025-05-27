using System;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Attribute
{
    public class ChangeMaxValue : AttributeEffectStrategy
    {
        protected override void OnStartEffect(IAttribute atr)
        {
            atr.MaxValueModifier(GetAmount(atr.StartMaxValue));
        }

        protected override void OnStopEffect(IAttribute atr)
        {
            atr.MaxValueModifier(-GetAmount(atr.StartMaxValue));
        }

        
    }
}