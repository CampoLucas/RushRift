using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class MovementConfig : MotionConfig
    {
        public float Speed => speed;
        public float MaxSpeed => maxSpeed;
        public float Drag => drag;
        public float MinThreshold => minThreshold;
        public float MaxThreshold => maxThreshold;
        public float AirMultiplier => airMultiplier;
        public float AirForwardMultiplier => airForwardMultiplier;
        public float AirFallMultiplier => airFallMultiplier;
        public bool SlidingEnabled => slidingEnabled;
        public Sliding Sliding => sliding;
        
        [Header("General")]
        [SerializeField] private float speed = 5000f;
        [Tooltip("The max horizontal speed.")]
        [SerializeField] private float maxSpeed = 35f;
        
        [Header("Drag")]
        [Tooltip("Used to stop sliding on the ground.")]
        [SerializeField] private float drag = .15f;
        [SerializeField] private float minThreshold = 0.01f;
        [SerializeField] private float maxThreshold = 0.05f;

        [Header("Air")]
        [Tooltip("Multiplies the horizontal speed while mid-air.")]
        [Range(0, 2)] [SerializeField] private float airMultiplier = .7f;
        [Tooltip("Multiplies the horizontal forward speed while mid-air.")]
        [Range(0, 2)] [SerializeField] private float airForwardMultiplier = .7f;
        [Tooltip("Scales the fall speed.")]
        [Range(0, 1)] [SerializeField] private float airFallMultiplier = 0.005f;

        [Header("Slope Sliding")]
        [SerializeField] private bool slidingEnabled;
        [SerializeField] private Sliding sliding;
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            controller.TryAddHandler(new MovementHandler(this), rebuildHandlers);
        }
    }

    [System.Serializable]
    public class Sliding
    {
        public float MaxSlopeAng => maxSlopeAngle;
        public float Mult => multiplier;
        public float RecoverSpeed => recoverSpeed;
        public float SpeedScaling => speedScaling;
        public float SlideForce => slideForce;
        public bool KeepSlipperyValueOnAir => keepSlipperyValueOnAir;
        public bool RecoverOnAir => recoverOnAir;
        public float AirRecoverySpeed => airRecoverSpeed;
        public float MaxSpeedModifier => maxSpeedModifier;
        public float FallTriggerSpeed => fallTriggerSpeed;
        public float FallMaxAmount => fallMaxAmount;
        
        [Header("General")]
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float multiplier = .1f;
        [SerializeField] private float speedScaling = 30f;
        [SerializeField] private float slideForce = 160f;
        
        [Header("Recover")]
        [SerializeField] private float recoverSpeed = 3f;
        // ToDo: scaling when moving fast variables

        [Header("Air")]
        [SerializeField] private bool keepSlipperyValueOnAir;
        [SerializeField] private bool recoverOnAir;
        [SerializeField] private float airRecoverSpeed = 4f;

        [Header("Boosts")]
        [SerializeField] private float maxSpeedModifier = 3f;
        [SerializeField] private float fallTriggerSpeed = 30f;
        [SerializeField] private float fallMaxAmount = .1f;

    }
}