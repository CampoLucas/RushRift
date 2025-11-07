using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public interface ITrigger : ISubject, IPredicate<IController>
    {
        
    }
}