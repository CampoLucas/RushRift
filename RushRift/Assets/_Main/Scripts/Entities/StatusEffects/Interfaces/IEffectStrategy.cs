using System;

namespace Game.Entities
{
    public interface IEffectStrategy : IDisposable
    {
        void StartEffect(IController controller);
        void StopEffect(IController controller);
    }
}