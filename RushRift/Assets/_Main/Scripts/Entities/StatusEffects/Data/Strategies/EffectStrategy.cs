using UnityEngine;

namespace Game.Entities
{
    public abstract class EffectStrategy : ScriptableObject, IEffectStrategy
    {
        public abstract void StartEffect(IController controller);
        public abstract void StopEffect(IController controller);
        
        public void Dispose()
        {
            
        }

        public virtual string Description() => "";
    }
}