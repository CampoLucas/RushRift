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
        private IDashStrategy _strategy;
        
        private CharacterController _controller;
        private Transform _origin;
        private Transform _cameraTransform;

        private Vector3 _dashStartPosition;
        private Vector3 _dashEndPosition;
        private bool _isDashing;
        private float _dashStartTime;
        private float _nextDashTime;


        public DashComponent(CharacterController controller, Transform origin, Transform cameraTransform,
            IDashStrategy strategy, DashData data)
        {
            _controller = controller;
            _origin = origin;
            _cameraTransform = cameraTransform;

            _data = data;
            
            // Assign dash strategy
            _strategy = strategy;
            _updateObserver = new ActionObserver<float>(OnUpdate);
        }

        private void OnUpdate(float delta)
        {
            if (!_isDashing) return;

            var elapsed = Time.time - _dashStartTime;
            var progress = Mathf.Clamp01(elapsed / _data.Duration);
            var curveValue = _data.SpeedCurve.Evaluate(progress);
            
            // Lerp between start and end positions based on curve
            var currentPosition = Vector3.Lerp(_dashStartPosition, _dashEndPosition, curveValue);
            var moveVector = currentPosition - _origin.position;
                
            _controller.Move(moveVector); // Move by the difference

            if (progress >= 1f)
            {
                _isDashing = false;
                
                OnStopDash.NotifyAll();
            }
        }

        public bool CanDash(IController controller)
        {
            if (Time.time >= _nextDashTime && controller.GetModel().TryGetComponent<EnergyComponent>(out var energy))
            {
                return energy.Value >= _data.Cost;
            }

            Debug.Log("SuperTest: can't dash");
            return false;
        }

        public bool StartDash()
        {
            if (_isDashing) return false;
            
            _strategy.StartDash(_origin, _cameraTransform, out _dashStartPosition, out _dashEndPosition, out var dir);

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
            
            _strategy.Dispose();
            _strategy = null;
        }
    }
}