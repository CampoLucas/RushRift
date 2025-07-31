using System;

namespace Game.Entities.Components.MotionController.Strategies
{
    public interface IDashUpdateStrategy : IDisposable
    {
        void OnReset();
        bool OnDashUpdate(in MotionContext context, in float delta);
    }
}