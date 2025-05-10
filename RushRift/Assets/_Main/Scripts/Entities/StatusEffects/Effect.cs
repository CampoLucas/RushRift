using System.Linq;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Status Effects/Effect")]
    public class Effect : ScriptableObject
    {
        [Header("Effects")]
        [SerializeField] private SerializableSOCollection<EffectStrategy> strategy;
        
        [Header("Triggers")]
        [SerializeField] private SerializableSOCollection<EffectTrigger> startTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> stopTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> removeTriggers;

        public void ApplyEffect(IController controller)
        {
            var startTr = startTriggers.Select(a => a.GetTrigger(controller)).ToArray();
            var stopTr = stopTriggers.Select(a => a.GetTrigger(controller)).ToArray();
            var removeTr = removeTriggers.Select(a => a.GetTrigger(controller)).ToArray();

            var effectInstance = new EffectInstance(strategy.Get<IEffectStrategy>().ToArray(), startTr, stopTr, removeTr);
            effectInstance.Initialize(controller);
        }
    }
}