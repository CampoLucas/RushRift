using System;
using Game.Entities;

namespace Game.Levels
{
    [Serializable]
    public struct Medal
    {
        public string EffectName => src != UpgradeSource.Self || upgrade == null ? "" : upgrade.EffectName; 
        
        public float requiredTime;
        public UpgradeSource src;
        public Effect upgrade;
    }

    public enum UpgradeSource
    {
        None,
        Self,
        Child, // for rushes and arcades uses the upgrade of the children levels
    }
}