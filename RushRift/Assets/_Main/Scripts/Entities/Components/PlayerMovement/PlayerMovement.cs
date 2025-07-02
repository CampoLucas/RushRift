using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Detection;
using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components
{
    public class PlayerMovement : IMovement
    {
        public Vector3 Velocity { get; private set; }
        public bool Grounded => _grounded || _coyoteTimer > 0;
        public float MaxSpeed => _currentProfile ? _currentProfile.Get().MaxSpeed + _speedModifier : 0;
        public float BaseMaxSpeed => _currentProfile ? _currentProfile.Get().MaxSpeed : 0;
        
        private PlayerMovementData _data;
        private CharacterController _controller;
        private Transform _orientation;
        private Transform _origin;
        private IDetection _detection;
        
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private Vector3 _moveInput;
        private Vector3 _prevPosition;
        private float _moveAmount;
        
        private float _accelTimer;
        private float _decTimer;
        private float _coyoteTimer;
        private bool _grounded;
        private bool _gravity = true;

        private float _speedModifier;

        private IObserver<float> _tick;
        private IObserver<float> _lateTick;

        private NullCheck<MovementProfile> _currentProfile;
        private Dictionary<MoveType, MovementProfile> _profilesDictionary;

        public PlayerMovement(PlayerMovementData data, CharacterController controller, Transform origin, Transform orientation)
        {
            _data = data;
            _controller = controller;
            _orientation = orientation;
            _origin = origin;
            _detection = _data.Detector.Get(origin);
            _tick = new ActionObserver<float>(Tick);
            _lateTick = new ActionObserver<float>(LateTick);

            data.TryCreateProfilesDictionary(out _profilesDictionary);
            SetProfile(MoveType.Grounded);
        }

        private void Tick(float delta)
        {
            GroundChecks(delta);
            //HandleInput();
            HandleMovement(delta);

            var pos = _origin.position;
            Velocity = (_prevPosition - pos) / delta;
            _moveAmount = Velocity.magnitude;
            _prevPosition = pos;
        }

        private void LateTick(float delta)
        {
            _moveDirection = Vector3.zero;
        }
        
        public void AddMoveDir(Vector3 dir, bool normalize = true)
        {
#if true
            if (normalize) dir.Normalize();

            if (_orientation != null)
            {
                var forward = _orientation.forward.XOZ().normalized;
                var right = _orientation.right.XOZ().normalized;

                _moveDirection = (forward * dir.z + right * dir.x).normalized;
            }
            else
            {
                _moveDirection = dir;
            }
            
            
#else
            _moveInput = dir;
            if (normalize)
            {
                _moveInput.Normalize();
            }
#endif
        }

        public void Move(Vector3 dir, float delta)
        {
            _controller.Move(dir * delta);
        }
        
        public void ApplyImpulse(Vector3 impulse)
        {
            _velocity = impulse;
        }

        public void AppendMaxSpeed(float amount) => _speedModifier += amount;
        public float MoveAmount() => _moveAmount;

        public void EnableGravity(bool value)
        {
            _gravity = value;
        }

        public void SetYVelocity(float velocity)
        {
            _velocity.y = velocity;
        }

        public void SetProfile(MoveType type)
        {
            if (_profilesDictionary.TryGetValue(type, out var newProfile))
            {
                _currentProfile = newProfile;
            }
        }

        #region Component Methods

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _tick;
            return true;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = _lateTick;
            return true;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        #endregion

        #region Debug Methods

        public void OnDraw(Transform origin)
        {
            if (_detection != null)
            {
                _detection.Draw(origin, new Color(.5f, .5f, .5f, .1f));
            }
        }

        public void OnDrawSelected(Transform origin)
        {
            if (_detection != null)
            {
                _detection.Draw(origin, _detection.IsColliding ? Color.green : Color.red);
            }
        }

        #endregion

        private void GroundChecks(float delta)
        {
            var wasGrounded = _grounded;

            _grounded = _detection.Detect();

            if (_grounded)
            {
                _coyoteTimer = _data.CoyoteTime;
            }
            else if (wasGrounded)
            {
                _coyoteTimer -= delta;
                _velocity.y = -2f;
            }
            else
            {
                _coyoteTimer -= delta;
            }
            
            _coyoteTimer = Mathf.Clamp(_coyoteTimer, 0f, _data.CoyoteTime);
        }

        // private void HandleInput()
        // {
        //     var forward = Orientation.forward.XOZ().normalized;
        //     var right = Orientation.right.XOZ().normalized;
        //
        //     _moveDirection = (forward * _moveInput.z + right * _moveInput.x).normalized;
        // }

        private void HandleMovement(float delta)
        {
            var horizontalVel = _velocity.XOZ();
            var targetVel = _moveDirection * MaxSpeed;

            var moveMultiplier = _currentProfile.Get().Control;
            targetVel *= moveMultiplier;

            var deltaVel = targetVel - horizontalVel;

            var moveMagnitude = _moveDirection.magnitude;
            var isMoving = moveMagnitude > 0.01f;

    
            // Calculates the velocity when acceleration or decelerating 
            if (isMoving)
            {
                CalculateDeltaVelocity(ref deltaVel, _currentProfile.Get().Accel, _currentProfile.Get().AccelCurve, ref _accelTimer, delta);
            }
            else
            {
                CalculateDeltaVelocity(ref deltaVel, _currentProfile.Get().Dec, _currentProfile.Get().DecCurve, ref _decTimer, delta);
            }

            horizontalVel += deltaVel;

            // Calculates the velocity y
            if (_gravity)
            {
                _velocity.y += _data.Gravity * delta;
            }
            

            _velocity = new Vector3(horizontalVel.x, _velocity.y, horizontalVel.z);
            _controller.Move(_velocity * delta);

            // Reset vertical velocity if grounded
            if (Grounded && _velocity.y < 0f)
            {
                _velocity.y = -9.8f;
            }
        }

        private void CalculateDeltaVelocity(ref Vector3 deltaVel, float rate, AnimationCurve curve, ref float timer, float delta)
        {
            timer += delta;
            var curveFactor = curve.Evaluate(Mathf.Clamp01(timer));

            deltaVel = Vector3.ClampMagnitude(deltaVel, rate * delta * curveFactor);
        }
        
        public void Dispose()
        {
            _data = null;
            _controller = null;
            _orientation = null;
            _origin = null;
            
            _detection?.Dispose();
            _detection = null;
            
            _tick?.Dispose();
            _tick = null;
            
            _lateTick?.Dispose();
            _lateTick = null;

            _currentProfile = null;
            
            _profilesDictionary?.Clear();
            _profilesDictionary = null;
        }
    }
}