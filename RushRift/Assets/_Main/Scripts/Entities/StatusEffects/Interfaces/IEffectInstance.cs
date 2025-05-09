using System;

namespace Game.Entities
{
    public interface IEffectInstance : IDisposable
    {
        void Initialize(IController controller);
    }
}