using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class DashComponent : IEntityComponent
    {
        public ISubject OnStartDash { get; private set; } = new Subject();
        public ISubject OnStopDash { get; private set; } = new Subject();
        public float Cost => _data.Cost;

        private DashData _data;
        private IObserver<float> _updateObserver;
        private IDashStartStrategy _startStrategy;
        private IDashUpdateStrategy _updateStrategy;
        
        private CharacterController _controller;
        private Transform _origin;
        private Transform _cameraTransform;

        private Vector3 _dashStartPosition;
        private Vector3 _dashEndPosition;
        private bool _isDashing;
        private float _dashStartTime;
        private float _nextDashTime;

        private Vector3 _startPos;
        private Vector3 _endPos;

        private IMovement _movement;
        
        public DashComponent(CharacterController controller, Transform origin, Transform cameraTransform,
            IDashStartStrategy startStrategy, DashData data, IMovement movement)
        {
            _controller = controller;
            _origin = origin;
            _cameraTransform = cameraTransform;

            _data = data;
            
            // Assign dash strategy
            _startStrategy = startStrategy;
            _updateObserver = new ActionObserver<float>(OnUpdate);

            _movement = movement;
        }

        private void OnUpdate(float delta)
        {
            if (!_isDashing) return;

            var elapsed = Time.time - _dashStartTime;
            var progress = Mathf.Clamp01(elapsed / _data.Duration);
            var curveValue = _data.SpeedCurve.Evaluate(progress);
            
            // Lerp between start and end positions based on curve
            var currentPosition = Vector3.Lerp(_dashStartPosition, _dashEndPosition, curveValue);
            var position = _origin.position;
            var moveVector = currentPosition - position;
                
            _controller.Move(moveVector); // Move by the difference
            //_updateStrategy?.OnDashUpdate(_origin, position);

            if (_isDashing && (progress >= 1f || (_updateStrategy != null && _updateStrategy.OnDashUpdate(_origin, position))))
            {
                _isDashing = false;
                _endPos = _origin.position;
                
                //_movement.ApplyImpulse((_endPos - _startPos) / delta); 
                _movement.ApplyImpulse(((_endPos - _startPos) / elapsed) * _data.Dampening);
                
                OnStopDash.NotifyAll();
                _updateStrategy?.Reset();
            }
        }

        public bool CanDash(IController controller)
        {
            if (Time.time >= _nextDashTime && controller.GetModel().TryGetComponent<EnergyComponent>(out var energy))
            {
                return energy.Value >= _data.Cost;
            }

            return false;
        }

        public bool StartDash()
        {
            if (_isDashing) return false;

            _startPos = _origin.position;
            _startStrategy.StartDash(_origin, _cameraTransform, out _dashStartPosition, out _dashEndPosition, out var dir);

            if (_dashStartPosition != _dashEndPosition)
            {
                _isDashing = true;
                _dashStartTime = Time.time;
                _nextDashTime = Time.time + _data.Cooldown;
                
                OnStartDash.NotifyAll();
                return true;
            }

            return false;
        }

        public void SetUpdateStrategy(IDashUpdateStrategy updateStrategy)
        {
            var isCurrentNull = _updateStrategy == null;

            if (!isCurrentNull)
            {
                if (_updateStrategy == updateStrategy) return;
                _updateStrategy.Dispose();
            }

            _updateStrategy = updateStrategy;
        }
        
        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _updateObserver;
            return true;
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
            
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
        
        public void Dispose()
        {
            OnStartDash.DetachAll();
            OnStartDash.Dispose();
            OnStartDash = null;
            
            OnStopDash.DetachAll();
            OnStopDash.Dispose();
            OnStopDash = null;

            _data = null;
            _controller = null;
            _origin = null;
            _cameraTransform = null;
            
            _updateObserver.Dispose();
            _updateObserver = null;
            
            _startStrategy.Dispose();
            _startStrategy = null;

            _movement = null;
        }
    }
}