using System;
using Game.Entities;

namespace Game.Levels
{
    [Serializable]
    public struct Medal
    {
        public float requiredTime;
        public Effect upgrade;
    }
}