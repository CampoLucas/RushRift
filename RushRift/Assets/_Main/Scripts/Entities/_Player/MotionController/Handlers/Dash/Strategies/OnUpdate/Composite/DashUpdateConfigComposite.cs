using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashUpdateConfigComposite
    {
        [SerializeField] private DashUpdateEnum strategy;

        [Header("Settings")]
        [SerializeField] private DashDamageConfig damageConfig;

        public DashUpdateStrategyComposite GetStrategy()
        {
            var composite = new DashUpdateStrategyComposite();

            if ((strategy & DashUpdateEnum.Damage) != 0) composite.Add(GetDamageStrat());

            return composite;
        }

        public DashDamageStrategy GetDamageStrat() => new DashDamageStrategy(damageConfig);
    }
}