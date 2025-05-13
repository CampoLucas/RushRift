using Game.DesignPatterns.Observers;
using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    public class Movement : IMovement
    {
        public Vector3 Velocity { get; private set; }
        public bool Grounded => _isGrounded;
        public float MaxSpeed => _data.MaxSpeed + _speedModifier;
        public float BaseMaxSpeed => _data.MaxSpeed;

        private IObserver<float> _updateObserver;
        private IObserver<float> _lateUpdateObserver;
        
        // References
        private MovementData _data;
        private CharacterController _controller;
        private Transform _transform;

        // Movement variables
        private Vector3 _moveDir;
        private Vector3 _currentVelocity;
        private Vector3 _prevMoveDir;
        private float _verticalVelocity;
        private float _accelTime = 0f;
        private float _decelTime = 0f;
        private float _moveAmount;


        // Collision detection
        private BoxOverlapDetect _groundDetect;
        private bool _isGrounded;

        // Velocity
        private Vector3 _prevPosition;
        private float _speedModifier;

        private bool _enableGravity;

        public Movement(CharacterController controller, MovementData data)
        {
            _controller = controller;
            _transform = controller.transform;
            
            SetData(data);
            _updateObserver = new ActionObserver<float>(Update);
        }

        public void Update(float delta)
        {
            CheckGrounded();


            _verticalVelocity += _data.Gravity.GetValue() * delta;
            if (_verticalVelocity > _data.Gravity.MaxGravityAccel)
            {
                _verticalVelocity = _data.Gravity.MaxGravityAccel;
            }
            
            if (_isGrounded)
            {
                //_verticalVelocity = -1f;
                Move(_moveDir, _data.GroundAccel, _data.GroundDec, delta);
            }
            else
            {
                Move(_moveDir, _data.AirAccel, _data.AirDec, delta);
            }
            
            _moveDir = Vector3.zero;
            
            var pos = _transform.position;
            Velocity = (_prevPosition - pos) / delta;
            _moveAmount = Velocity.magnitude;
            _prevPosition = pos;
        }

        #region Movement

        private void Move(Vector3 dir, float accel, float deccel, float delta)
        {
            var targetVelocity = dir * MaxSpeed;
            var horizontalVelocity = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
            var velocityDelta = targetVelocity - horizontalVelocity;

            var accelerating = dir.sqrMagnitude > 0.01f;

            float curveFactor;

            if (accelerating)
            {
                _accelTime += delta;
                _decelTime = 0f;
                curveFactor = _data.AccelerationCurve.Evaluate(Mathf.Clamp01(_accelTime));
            }
            else
            {
                _decelTime += delta;
                _accelTime = 0f;
                curveFactor = _data.DecelerationCurve.Evaluate(Mathf.Clamp01(_decelTime));
            }

            var rate = accelerating ? accel : deccel;
            
            velocityDelta = Vector3.ClampMagnitude(velocityDelta, rate * delta * curveFactor);

            horizontalVelocity += velocityDelta;

            if (!accelerating && horizontalVelocity.magnitude < .01f)
            {
                horizontalVelocity = Vector3.zero;
            }

            
            _currentVelocity = new Vector3(horizontalVelocity.x,  _enableGravity ? _verticalVelocity : 0, horizontalVelocity.z);
            _controller.Move(_currentVelocity * delta);
            _prevMoveDir = dir;
        }


        public void Move(Vector3 dir, float delta)
        {
            _controller.Move(dir * (delta));
        }
        
        private void CheckGrounded()
        {
            _isGrounded = _groundDetect.Detect();
        }

        #endregion
        
        public void AddMoveDir(Vector3 dir, bool normalize = true)
        {
            _moveDir += dir;
            if (normalize && _moveDir.magnitude > 1)
            {
                _moveDir.Normalize();
            }
        }

        public void AddImpulse(Vector3 dir)
        {
            
        }

        public void SetData(MovementData data)
        {
            _data = data;
            
            if (_groundDetect != null) _groundDetect.Dispose();
            _groundDetect = data.GetGroundDetector(_transform);
            
        }

        public void AppendMaxSpeed(float amount)
        {
            _speedModifier += amount;
        }

        public float MoveAmount()
        {
            return _moveAmount;
        }

        public void EnableGravity(bool value)
        {
            _enableGravity = value;
        }


        public void Dispose()
        {
            _data = null;
            _transform = null;
            _controller = null;
            
            _groundDetect.Dispose();
            _groundDetect = null;
            
            _updateObserver.Dispose();
            _updateObserver = null;
        }

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _updateObserver;
            return observer != null;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public void OnDraw(Transform origin)
        {
            _groundDetect.Draw(origin, _isGrounded ? Color.green : Color.red);
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
    }
}