using Game.DesignPatterns.Observers;
using Game.Detection;
using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components
{
    public class PlayerMovement : IMovement
    {
        public Transform Orientation { get; private set; }
        public Vector3 Velocity => _velocity;
        public bool Grounded => _grounded || _coyoteTimer > 0;
        public float MaxSpeed => _data.MaxSpeed + _speedModifier;
        public float BaseMaxSpeed => _data.MaxSpeed;
        
        private PlayerMovementData _data;
        private CharacterController _controller;
        private IDetection _detection;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private Vector3 _moveInput;
        private float _accelTimer;
        private float _decelTimer;
        private float _coyoteTimer;
        private bool _grounded;
        private bool _gravity;

        private float _speedModifier;

        private IObserver<float> _tick;
        private IObserver<float> _lateTick;

        public PlayerMovement(PlayerMovementData data, CharacterController controller, Transform origin, Transform orientation)
        {
            _data = data;
            _controller = controller;
            Orientation = orientation;
            _detection = _data.Detector.Get(origin);
            _tick = new ActionObserver<float>(Tick);
            _lateTick = new ActionObserver<float>(LateTick);
        }

        private void Tick(float delta)
        {
            GroundChecks(delta);
            //HandleInput();
            HandleMovement(delta);
        }

        private void LateTick(float delta)
        {
            _moveDirection = Vector3.zero;
        }
        
        public void AddMoveDir(Vector3 dir, bool normalize = true)
        {
#if true
            if (normalize) dir.Normalize();

            if (Orientation != null)
            {
                var forward = Orientation.forward.XOZ().normalized;
                var right = Orientation.right.XOZ().normalized;

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
        public float MoveAmount() => _velocity.magnitude;

        public void EnableGravity(bool value)
        {
            _gravity = value;
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
        
        
        public void Dispose()
        {
            _data = null;
            _controller = null;
            _detection?.Dispose();
            _detection = null;
            Orientation = null;
            _tick?.Dispose();
            _tick = null;
            _lateTick?.Dispose();
            _lateTick = null;
        }

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

            var moveMultiplier = Grounded ? 1f : _data.AirControl;
            targetVel *= moveMultiplier;

            var deltaVel = targetVel - horizontalVel;

            var moveMagnitude = _moveDirection.magnitude;
            var isMoving = moveMagnitude > 0.01f;


            if (isMoving)
            {
                CalculateDeltaVelocity(ref deltaVel, _data.AccelRate, _data.AccelCurve, ref _accelTimer, delta);
            }
            else
            {
                CalculateDeltaVelocity(ref deltaVel, _data.DecelRate, _data.DecelCurve, ref _decelTimer, delta);
            }

            horizontalVel += deltaVel;

            if (_gravity)
            {
                _velocity.y += _data.Gravity * delta;
            }
            

            _velocity = new Vector3(horizontalVel.x, _velocity.y, horizontalVel.z);
            _controller.Move(_velocity * delta);

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
    }
}