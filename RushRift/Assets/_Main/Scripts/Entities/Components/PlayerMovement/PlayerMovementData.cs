using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class PlayerMovementData
    {
        public float MaxSpeed => maxSpeed;
        public float AccelRate => acceleration;
        public AnimationCurve AccelCurve => accelerationCurve;
        public float DecelRate => deceleration;
        public AnimationCurve DecelCurve => decelerationCurve;
        public float AirControl => airControl;
        public float Gravity => gravity;
        public float CoyoteTime => coyoteTime;
        public IDetectionData Detector => detector;
        
        [Header("Settings")]
        [SerializeField] private float maxSpeed = 10f;

        [Space(-5), Header("Acceleration Settings")]
        [Tooltip("The rate it accelerates while moving.")]
        [SerializeField] private float acceleration = 15f;
        [Tooltip("Multiplies the acceleration rate. Only works properly if it accelerates in one second.")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Space(-5), Header("Deceleration Settings")]
        [Tooltip("The rate it accelerates while moving.")]
        [SerializeField] private float deceleration = 30f;
        [Tooltip("Multiplies the acceleration rate. Only works properly if it accelerates in one second.")]
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Space(10), Header("Air Settings")]
        [Tooltip("How much it can move in the air.")]
        [SerializeField, Range(0f, 1f)] private float airControl = .5f;
        [SerializeField] private float gravity = -15f;

        [Space(10), Header("Coyote Time")]
        [SerializeField, Range(0f, .5f)] private float coyoteTime = .15f;

        [Space(10), Header("Ground Check")]
        [SerializeField] private SphereOverlapData detector;

        public IMovement GetMovement(CharacterController controller, Transform origin, Transform orientation) =>
            new PlayerMovement(this, controller, origin, orientation);
    }
}