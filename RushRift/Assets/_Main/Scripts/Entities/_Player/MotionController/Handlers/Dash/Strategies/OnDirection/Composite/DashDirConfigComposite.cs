using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashDirConfigComposite
    {
        [SerializeField] private DashDirEnum strategy = DashDirEnum.Look;

        [Header("Settings")]
        [SerializeField] private DashDirConfig lookConfig;
        [SerializeField] private DashDirConfig inputConfig;
        [SerializeField] private DashDirConfig momentumConfig;
        [FormerlySerializedAs("targetConfig")] [SerializeField] private DashTargetConfig dashTargetConfig;
        
        
        public DashDirStrategyComposite GetStrategy()
        {
            var composite = new DashDirStrategyComposite();

            if ((strategy & DashDirEnum.Look) != 0) composite.Add(DashDirEnum.Look, GetLookStrat());
            if ((strategy & DashDirEnum.Input) != 0) composite.Add(DashDirEnum.Input,GetInputStrat());
            if ((strategy & DashDirEnum.Momentum) != 0) composite.Add(DashDirEnum.Momentum,GetMomentumDirStrat());
            if ((strategy & DashDirEnum.Target) != 0) composite.Add(DashDirEnum.Target,GetTargetDirStrat());

            return composite;
        }

        public DashLookStrategy GetLookStrat() => new DashLookStrategy(lookConfig);
        public DashInputStrategy GetInputStrat() => new DashInputStrategy(inputConfig);
        public DashMomentumStrategy GetMomentumDirStrat() => new DashMomentumStrategy(momentumConfig);
        public DashTargetStrategy GetTargetDirStrat() => new DashTargetStrategy(dashTargetConfig);
    }
}