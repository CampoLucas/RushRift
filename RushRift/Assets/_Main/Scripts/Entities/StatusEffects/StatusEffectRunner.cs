using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class StatusEffectRunner : IEntityComponent
    {
        private List<IEffectInstance> _effects = new();

        public void AddEffect(IEffectInstance effect)
        {
            _effects.Add(effect);
            //effect.OnAdd();
        }

        public void RemoveEffect(IEffectInstance effect)
        {
            _effects.Remove(effect);
            effect.Dispose();
            //effect.OnRemove();
        }

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = null;
            return false;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = null;
            return false;
        }

        public void OnDraw(Transform origin)
        {
            
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
        
        public void Dispose()
        {
            for (var i = 0; i < _effects.Count; i++)
            {
                var effect = _effects[i];
                if (effect == null) continue;

                effect.Dispose();
            }
            
            _effects.Clear();
            _effects = null;
        }
    }
}
