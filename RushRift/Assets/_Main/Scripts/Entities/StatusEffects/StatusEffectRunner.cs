using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public sealed class StatusEffectRunner : EntityComponent
    {
        private List<IEffectInstance> _effects = new();
        private ISubject<float> _updatableEffects = new Subject<float>();
        private IObserver<float> _updateObserver;

        public StatusEffectRunner()
        {
            _updateObserver = new ActionObserver<float>(OnUpdate);
        }
        
        private void OnUpdate(float delta)
        {
            _updatableEffects.NotifyAll(delta);
        }

        public void AddEffect(IEffectInstance effect)
        {
            _effects.Add(effect);
            
            if (effect.TryGetUpdate(out var observer))
            {
                _updatableEffects.Attach(observer);
            }
        }

        public void RemoveEffect(IEffectInstance effect)
        {
            // Detach the effect's update from the runner
            if (effect.TryGetUpdate(out var observer))
            {
                _updatableEffects.Detach(observer);
            }
            
            // Remove the effect from the effects list
            _effects.Remove(effect);
            effect.Dispose(); // Dispose all effect's reference, including subjects and observers created dynamically.
        }

        public override bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _updateObserver;
            return _updateObserver != null;
        }
        
        protected override void OnDispose()
        {
            for (var i = 0; i < _effects.Count; i++)
            {
                var effect = _effects[i];
                if (effect == null) continue;

                effect.Dispose();
            }
            
            _effects.Clear();
            _effects = null;
            
            _updatableEffects.Dispose();
            _updatableEffects = null;
            
            _updateObserver.Dispose();
            _updateObserver = null;
        }
    }
}
