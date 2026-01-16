using System;
using Game.Entities;
using Game.UI.Elements;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Levels
{
    [Serializable]
    public struct Medal
    {
        public string EffectName => src != UpgradeSource.Self || upgrade == null ? "" : upgrade.EffectName;
        public UpgradeIcon Icon => icon;
        
        public VideoClip EffectVideo =>
            src != UpgradeSource.Self || upgrade == null ? null : upgrade.PopUpVideo;

        public float requiredTime;
        public UpgradeSource src;
        public Effect upgrade;
        [SerializeField] private UpgradeIcon icon;
    }

    public enum UpgradeSource
    {
        None,
        Self,
        Child, // for rushes and arcades uses the upgrade of the children levels
    }
}