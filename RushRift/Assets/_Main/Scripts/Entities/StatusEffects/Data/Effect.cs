using System.Linq;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Status Effects/Effect")]
    public class Effect : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private float duration;
        [SerializeField] private bool removeWhenApplied = false;
        [SerializeField] private bool detachWhenApplied = true;
        
        [Header("Effects")]
        [SerializeField] private SerializableSOCollection<EffectStrategy> strategy;
        
        [Header("Triggers")]
        [SerializeField] private SerializableSOCollection<EffectTrigger> startTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> stopTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> removeTriggers;

        public void ApplyEffect(IController controller)
        {
            ApplyEffect(controller, duration);
        }
        
        public void ApplyEffect(IController controller, float dur)
        {
            var startTr = startTriggers.Select(a => a.GetTrigger(controller)).ToArray();
            var stopTr = stopTriggers.Select(a => a.GetTrigger(controller)).ToArray();
            var removeTr = removeTriggers.Select(a => a.GetTrigger(controller)).ToArray();
            var strategies = strategy.Get<IEffectStrategy>().ToArray();

            var effectInstance = dur > 0 ? 
                new EffectInstance(strategies, startTr, stopTr, removeTr, removeWhenApplied, detachWhenApplied, dur) :
                new EffectInstance(strategies, startTr, stopTr, removeTr, removeWhenApplied, detachWhenApplied);
            effectInstance.Initialize(controller);
        }

        
    }
}