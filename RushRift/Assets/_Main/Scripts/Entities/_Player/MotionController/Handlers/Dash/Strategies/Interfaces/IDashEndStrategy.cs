using System;

namespace Game.Entities.Components.MotionController.Strategies
{
    public interface IDashEndStrategy : IDisposable
    {
        void OnDashEnd(in MotionContext context);
    }
}