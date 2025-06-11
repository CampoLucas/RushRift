using System;
using UnityEngine;

namespace Game.Entities
{
    [System.Serializable]
    public class EffectGiver : IDisposable
    {
        [Header("Effect")]
        [SerializeField] private Effect effect;

        [Header("Settings")]
        [SerializeField] private bool overrideEffect;
        [SerializeField] private float duration;

        public void ApplyEffect(IController controller)
        {
            if (overrideEffect)
            {
                effect.ApplyEffect(controller, duration);
            }
            else
            {
                effect.ApplyEffect(controller);
            }
        }

        public bool HasEffect() => effect != null;
        
        public void Dispose()
        {
            effect = null;
        }
    }
}