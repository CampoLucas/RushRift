using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components.MotionController;
using Game.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities
{
    public class PlayerView : EntityView
    {
        [Header("Tick settings")]
        [SerializeField] private float tickTime = 0.1f;
        
        [Header("Move amount settings")]
        [SerializeField] private float minHorizontalSpeed = .5f;
        [SerializeField] private float maxHorizontalSpeed = 35;
        [SerializeField] private float minVerticalSpeed = .5f;
        [SerializeField] private float maxVerticalSpeed = 35;

        private static readonly int MoveAmount = Animator.StringToHash("MoveAmount");
        private static readonly int YMoveAmount = Animator.StringToHash("YMoveAmount");
        private static readonly int OnAirState = Animator.StringToHash("OnAirState");
        private static readonly int OnGroundState = Animator.StringToHash("OnGroundState");
        private Rigidbody _rigidbody;
        private RateLimiter _tickLimiter;
        private MotionController _motion;
        private ActionObserver<bool> _groundedObserver;

        private void OnEnable()
        {
            if (_tickLimiter == null) _tickLimiter = new RateLimiter(tickTime);
            _tickLimiter.Resume();
        }

        private void OnDisable()
        {
            _tickLimiter.Pause();
        }

        private void Awake()
        {
            _tickLimiter = new RateLimiter(tickTime);
            _rigidbody = GetComponent<Rigidbody>();
            _groundedObserver = new ActionObserver<bool>(OnGroundedHandler);
        }

        private void Start()
        {
            if (TryGetComponent<IController>(out var controller) &&
                controller.GetModel().TryGetComponent<MotionController>(out var motion))
            {
                _motion = motion;
                
                motion.AttachOnGrounded(_groundedObserver);
            }
        }

        private void Update()
        {
            if (!_tickLimiter.CanCall(out var delta)) return;

            var velocity = _rigidbody.velocity;
            var horizontalVelocity = velocity.XOZ();
            var speed = horizontalVelocity.magnitude;
            var moveAmount = GetMoveAmount(speed, minHorizontalSpeed, maxHorizontalSpeed);
            var yMoveAmount = GetMoveAmount(velocity.y, minVerticalSpeed, maxVerticalSpeed, -1, 1);
            
            SetFloat(MoveAmount, moveAmount);
            SetFloat(YMoveAmount, yMoveAmount);
        }

        private float GetMoveAmount(float speed, float minSpeed, float maxSpeed, float minValue = 0, float maxValue = 1)
        {
            return Mathf.Clamp((speed - minSpeed) / (maxSpeed - minSpeed), minValue, maxValue);
        }

        private void OnGroundedHandler(bool onGrounded)
        {
            Play(onGrounded ? OnGroundState : OnAirState);
            //SetBool(OnAir, !onGrounded);
            // if (onGrounded)
            // {
            //     Debug.Log("SuperTest: Grounded");
            // }
            // else
            // {
            //     Debug.Log("SuperTest: Not Grounded");
            // }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _rigidbody = null;
            _tickLimiter = null;
            
            _groundedObserver.Dispose();
        }
    }
}