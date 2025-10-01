using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(fileName = "EnableTerminals", menuName = "Game/Effects/Strategies/Enable Terminals")]
    public class EnableTerminals : EffectStrategy
    {
        [Header("Behavior")]
        [SerializeField, Tooltip("If true, sets LevelManager.CanUseTerminal = true on StartEffect.")]
        private bool enableOnStart = true;

        [SerializeField, Tooltip("If true, sets LevelManager.CanUseTerminal = false on StopEffect.")]
        private bool disableOnStop = false;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logs.")]
        private bool isDebugLoggingEnabled = false;

        public override void StartEffect(IController controller)
        {
            Game.LevelManager.CanUseTerminal = enableOnStart;
            if (isDebugLoggingEnabled)
                Debug.Log($"[EnableTerminals] StartEffect -> CanUseTerminal={Game.LevelManager.CanUseTerminal}");
        }

        public override void StopEffect(IController controller)
        {
            if (!disableOnStop) return;
            Game.LevelManager.CanUseTerminal = false;
            if (isDebugLoggingEnabled)
                Debug.Log($"[EnableTerminals] StopEffect -> CanUseTerminal={Game.LevelManager.CanUseTerminal}");
        }
    }
}