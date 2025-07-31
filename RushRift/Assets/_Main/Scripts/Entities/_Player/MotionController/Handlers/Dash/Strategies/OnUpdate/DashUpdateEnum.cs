using System;

namespace Game.Entities.Components.MotionController.Strategies
{
    [Flags]
    public enum DashUpdateEnum
    {
        Damage = 1 << 0,
    }
}