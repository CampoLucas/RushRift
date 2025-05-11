using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public abstract class EffectTrigger : SerializableSO, IPredicate<IController>
    {
        public abstract Trigger GetTrigger(IController controller);
        public abstract bool Evaluate(ref IController args);

        public void Dispose()
        {
            
        }
    }
}