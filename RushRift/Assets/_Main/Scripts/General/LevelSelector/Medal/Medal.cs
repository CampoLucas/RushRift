using System;
using Game.Entities;
using UnityEngine.Video; // NEW

namespace Game.Levels
{
    [Serializable]
    public struct Medal
    {
        public string EffectName =>
            src != UpgradeSource.Self || upgrade == null ? "" : upgrade.EffectName;
        
        public VideoClip EffectVideo =>
            src != UpgradeSource.Self || upgrade == null ? null : upgrade.PopUpVideo;

        public float requiredTime;
        public UpgradeSource src;
        public Effect upgrade;
    }

    public enum UpgradeSource
    {
        None,
        Self,
        Child,
    }
}