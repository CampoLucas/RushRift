using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashUpdateConfigComposite
    {
        [SerializeField] private DashUpdateEnum strategy;

        [Header("Settings")]
        [SerializeField] private DashDamageConfig damageConfig;
    }
}