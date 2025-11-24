using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public abstract class EffectTrigger : ScriptableObject, IPredicate<IController>
    {
        public abstract Trigger GetTrigger(IController controller);
        public abstract bool Evaluate(ref IController args);

        public void Dispose()
        {
            
        }
    }
}