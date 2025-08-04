using System;

namespace Game.Entities.Components.MotionController.Strategies
{
    [Flags]
    public enum DashDirEnum
    {
        Look         = 1 << 0,
        Input        = 1 << 1,
        Momentum     = 1 << 2,
        Target       = 1 << 3,
        //LastMove     = 1 << 4,
        //Fixed        = 1 << 5,
        //Escape       = 1 << 6,
    }
}