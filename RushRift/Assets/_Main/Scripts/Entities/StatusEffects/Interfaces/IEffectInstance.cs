using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public interface IEffectInstance : System.IDisposable
    {
        void Initialize(IController controller);
        bool TryGetUpdate(out IObserver<float> observer);
    }
}