using System;
using Game.Entities.Components.MotionController.Strategies;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class DashConfig : MotionConfig
    {
        public float Force => force;
        public float Duration => duration;
        public float MomentumMult => momentumMultiplier;
        public float Cooldown => cooldown;
        public float Cost => cost;
        
        [Header("General")]
        [SerializeField] private float force = 120f;
        [SerializeField] private float duration = .25f;
        [SerializeField] private float cooldown = .25f;
        
        [Header("Momentum")]
        [SerializeField] private float momentumMultiplier = .25f;

        [Header("Usage")]
        [SerializeField] private float cost = 1;

        [Header("Strategy")]
        [SerializeField] private DashDirStrategy _dirStrategy;
        //[SerializeField] private DashUpdateStrategy _updateStrategy;
        //[SerializeField] private DashEndStrategy _dirStrategy;
        
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            // create the composite strategies here...
            
            controller.TryAddHandler(new DashHandler(this, GetDirStrategy(), GetUpdateStrategy(), GetEndStrategy()), rebuildHandlers);
        }

        private CompositeDashDirStrategy GetDirStrategy()
        {
            var strategy = new CompositeDashDirStrategy();

            if ((_dirStrategy & DashDirStrategy.Look) != 0) strategy.Add(new LookDirStrategy());
            if ((_dirStrategy & DashDirStrategy.Input) != 0) strategy.Add(new InputDirStrategy());
            if ((_dirStrategy & DashDirStrategy.Momentum) != 0) strategy.Add(new MomentumDirStrategy());

            return strategy;
        }

        private CompositeDashUpdateStrategy GetUpdateStrategy()
        {
            var strategy = new CompositeDashUpdateStrategy();

            return strategy;
        }

        private CompositeDashEndStrategy GetEndStrategy()
        {
            var strategy = new CompositeDashEndStrategy();

            return strategy;
        }
    }

    [Flags]
    public enum DashDirStrategy
    {
        Look         = 1 << 0,
        Input        = 1 << 1,
        Momentum     = 1 << 2,
        //Target       = 1 << 3,
        //LastMove     = 1 << 4,
        //Fixed        = 1 << 5,
        //Escape       = 1 << 6,
    }
}