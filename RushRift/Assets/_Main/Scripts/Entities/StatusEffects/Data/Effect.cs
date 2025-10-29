using System.Linq;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Status Effects/Effect")]
    public class Effect : ScriptableObject
    {
        public string EffectName => effectName;
        
        [Header("Settings")]
        [SerializeField] private string effectName;
        [SerializeField] private float duration;
        [SerializeField] private bool removeWhenApplied = false;
        [SerializeField] private bool detachWhenApplied = true;
        
        [Header("Effects")]
        [SerializeField] private SerializableSOCollection<EffectStrategy> strategy;
        
        [Header("Triggers")]
        [SerializeField] private SerializableSOCollection<EffectTrigger> startTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> stopTriggers;
        [SerializeField] private SerializableSOCollection<EffectTrigger> removeTriggers;

//         public EffectInstance ApplyEffect(IController controller)
//         {
// #if UNITY_EDITOR
//             Debug.Log($"SuperTest: Applied effect {name}");
// #endif
//             
//             return ApplyEffect(controller, duration);
//         }
//         
        // public EffectInstance ApplyEffect(IController controller, float dur)
        // {
        //     var startTr = startTriggers.Select(a => a.GetTrigger(controller)).ToArray();
        //     var stopTr = stopTriggers.Select(a => a.GetTrigger(controller)).ToArray();
        //     var removeTr = removeTriggers.Select(a => a.GetTrigger(controller)).ToArray();
        //     var strategies = strategy.Get<IEffectStrategy>().ToArray();
        //
        //     var effectInstance = dur > 0 ? 
        //         new EffectInstance(strategies, startTr, stopTr, removeTr, removeWhenApplied, detachWhenApplied, dur) :
        //         new EffectInstance(strategies, startTr, stopTr, removeTr, removeWhenApplied, detachWhenApplied);
        //     effectInstance.Initialize(controller);
        //
        //     return effectInstance;
        // }

        public EffectInstance ApplyEffect(IController controller, 
            float dur = -1, 
            Trigger[] start = null, 
            Trigger[] stop = null, 
            Trigger[] remove = null)
        {
#if UNITY_EDITOR
            var overload = start != null || stop != null || remove != null ? "(overloaded)" : "";
            Debug.Log($"SuperTest: Applied effect {name}{overload}");
#endif
            
            var startTr = this.startTriggers.Select(a => a.GetTrigger(controller)).ToList();
            if (start is { Length: > 0 })
            {
                startTr.AddRange(start);
            }
            
            var stopTr = stopTriggers.Select(a => a.GetTrigger(controller)).ToList();
            if (stop is { Length: > 0 })
            {
                stopTr.AddRange(stop);
            }
            
            var removeTr = removeTriggers.Select(a => a.GetTrigger(controller)).ToList();
            if (remove is { Length: > 0 })
            {
                removeTr.AddRange(remove);
            }
            
            var strategies = strategy.Get<IEffectStrategy>().ToArray();

            var effectInstance = dur > 0 ? 
                new EffectInstance(strategies, startTr.ToArray(), stopTr.ToArray(), removeTr.ToArray(), removeWhenApplied, detachWhenApplied, dur) :
                new EffectInstance(strategies, startTr.ToArray(), stopTr.ToArray(), removeTr.ToArray(), removeWhenApplied, detachWhenApplied);
            effectInstance.Initialize(controller);

            return effectInstance;
        }

    }
}