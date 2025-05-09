using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public abstract class EffectTrigger : SerializableSO
    {
        public abstract ISubject GetSubject(IController controller);
    }
}