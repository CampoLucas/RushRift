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
        public float ShakeDur => shakeDuration;
        public float Blur => shakeDuration;
        public float ShakeMag => shakeMagnitude;
        public float BlurMag => blurMagnitude;
        
        [Header("General")]
        [SerializeField] private float force = 120f;
        [SerializeField] private float duration = .25f;
        [SerializeField] private float cooldown = .25f;
        
        [Header("Momentum")]
        [SerializeField] private float momentumMultiplier = .25f;

        [Header("Usage")]
        [SerializeField] private float cost = 1;

        [Header("Strategies")]
        [SerializeField] private DashDirConfigComposite dirStrategy;
        [SerializeField] private DashUpdateConfigComposite updateStrategy;

        [Header("Effects")]
        [SerializeField] private float shakeDuration;
        [SerializeField] private float shakeMagnitude;
        [SerializeField] private float blurMagnitude;

        
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            // create the composite strategies here...
            
            controller.TryAddHandler(new DashHandler(this, dirStrategy.GetStrategy(), updateStrategy.GetStrategy(), GetEndStrategy()), rebuildHandlers);
        }

        private CompositeDashEndStrategy GetEndStrategy()
        {
            var strategy = new CompositeDashEndStrategy();

            return strategy;
        }
    }

    

    
    //
    // [Flags]
    // public enum DashEndStrategy
    // {
    //     TransferMomentum = 1 << 0,
    // }
}