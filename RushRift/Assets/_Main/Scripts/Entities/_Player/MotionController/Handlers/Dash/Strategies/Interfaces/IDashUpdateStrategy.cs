using System;

namespace Game.Entities.Components.MotionController.Strategies
{
    public interface IDashUpdateStrategy : IDisposable
    {
        bool OnDashUpdate(in MotionContext context, in float delta);
    }
}