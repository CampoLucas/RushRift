using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Upgrades
{
    [DisallowMultipleComponent]
    public class EnableLockOnBlink : EffectStrategy
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Enable logs when the effect starts or stops.")]
        private bool isDebugLoggingEnabled = false;

        public override void StartEffect(IController controller)
        {
            LevelManager.CanUseLockOnBlink = true;
            if (isDebugLoggingEnabled) Debug.Log("[EnableLockOnBlink] StartEffect", this);
        }

        public override void StopEffect(IController controller)
        {
            LevelManager.CanUseLockOnBlink = false;
            if (isDebugLoggingEnabled) Debug.Log("[EnableLockOnBlink] StopEffect", this);
        }
    }
}