using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class MovementData
    {
        public float MaxSpeed => maxSpeed;
        public float GroundAccel => groundAcceleration;
        public float GroundDec => groundDeceleration;
        public float AirAccel => airAcceleration;
        public float AirDec => airDeceleration;
        public GravityData Gravity => gravity;
        
        [Header("Advanced")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public AnimationCurve AccelerationCurve => accelerationCurve;
        public AnimationCurve DecelerationCurve => decelerationCurve;
        
        [Header("Settings")] 
        [Range(1f, 100f)][SerializeField] private float maxSpeed = 10f;
        
        [Header("Ground")]
        [Range(0.25f,100f)][SerializeField] private float groundAcceleration = 5f;
        [Range(0.25f, 100f)][SerializeField] private float groundDeceleration = 20f;
        
        [Header("Air")]
        [Range(0.25f, 100f)][SerializeField] private float airAcceleration = 5f;
        [Range(0.25f, 100f)][SerializeField] private float airDeceleration = 5f;
        
        [Header("Ground Checks")]
        [SerializeField] private BoxOverlapDetectData groundDetect;

        [Header("Gravity")]
        [SerializeField] private GravityData gravity;
        
        public IMovement GetMovement(CharacterController controller) => new Movement(controller, this);
        public BoxOverlapDetect GetGroundDetector(Transform origin) => groundDetect.Get(origin);
    }
}