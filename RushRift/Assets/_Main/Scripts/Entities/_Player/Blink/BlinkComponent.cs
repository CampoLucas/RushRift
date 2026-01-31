using Game.DesignPatterns.Observers;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Android;

namespace Game.Entities.Components
{
    public class BlinkComponent : EntityComponent
    {
        public enum BlinkState { Idle, Charging, Charged, Finished }

        #region Public Properties

        public BlinkState State { get; private set; } = BlinkState.Idle;
        public NullCheck<Transform> CurrentTarget => _currentTarget;
        public Transform Origin => _origin.Get();
        public float BlinkProgress { get; private set; }

        #endregion
        
        #region Subjects

        public Subject OnTargetFound { get; } = new();
        public Subject OnTargetChanged { get; } = new();
        public Subject OnTargetLost { get; } = new();
        public Subject<float> OnProgressUpdated { get; } = new();
        public Subject OnBlinkStart { get; } = new();
        public Subject OnBlinkEnd { get; } = new();
        public Subject OnBlinkCanceled { get; } = new();

        #endregion

        private readonly BlinkConfig _config;
        
        private NullCheck<TargetDetectComp> _detector;
        private NullCheck<Transform> _origin;
        private NullCheck<Transform> _forward;
        
        private NullCheck<Transform> _currentTarget;
        
        private bool _targetLost;
        private float _targetLostTime;
        
        private float _cooldown;

        private ActionObserver<float> _updateObserver;
        
        // detector observers
        private ActionObserver<Transform> _onAimFound;
        private ActionObserver<Transform> _onAimLost;
        private ActionObserver<Transform> _onAimChanged;

        public BlinkComponent(BlinkConfig config, TargetDetectComp detector, Transform origin, Transform forward)
        {
            _config = config;
            _detector = detector;
            _origin = origin;
            _forward = forward;

            OnLoadingObserver = new NullCheck<ActionObserver<bool>>(new ActionObserver<bool>(OnLoadingHandler));

            AttachDetector(detector);
        }

        public BlinkComponent(BlinkConfig config, TargetDetectComp detector) : this(config, detector, detector.Origin,
            detector.Forward) { }
        
        private void Update(float delta)
        {
            if (!_origin || !_forward) return;

            if (_targetLost) DoGrace();

            if (State == BlinkState.Charging)
            {
                if (!_currentTarget)
                {
                    CancelCharge(hard: false);
                    return;
                }

                BlinkProgress += delta / Mathf.Max(0.0001f, _config.LockTime);
                
                if (BlinkProgress >= 1f)
                {
                    BlinkProgress = 1f;
                    State = BlinkState.Charged;
                    
                    OnProgressUpdated.NotifyAll(BlinkProgress);
                    OnBlinkEnd.NotifyAll(); // finished charging
                    return;
                }
                
                OnProgressUpdated.NotifyAll(BlinkProgress);
            }
        }

        private void OnLoadingHandler(bool state)
        {
            this.Log("Reset Blink On Load");
            HardReset();
        }

        #region Detector Callbacks

        private void OnDetectorTargetFound(Transform t)
        {
            _targetLost = false;
            _targetLostTime = 0f;

            if (_currentTarget.TryGet(out var curr))
            {
                if (curr != t)
                {
                    _currentTarget.Set(t);
                    OnTargetChanged.NotifyAll();
                }
            }
            else
            {
                _currentTarget.Set(t);
                OnTargetFound.NotifyAll();
            }
            
            //UpdateBlinkData();
        }

        private void OnDetectorTargetChanged(Transform t)
        {
            _targetLost = false;
            _targetLostTime = 0f;
            
            _currentTarget.Set(t);
            OnTargetChanged.NotifyAll();
            //UpdateBlinkData();
        }
        
        private void OnDetectorTargetLost(Transform t)
        {
            _targetLost = true;
            _targetLostTime = Time.time;
        }

        #endregion

        #region Charge Control

        public bool BeginCharge()
        {
            if (State != BlinkState.Idle) return false;
            if (Time.time < _cooldown) return false;
            if (!_currentTarget) return false;

            State = BlinkState.Charging;
            BlinkProgress = 0f;
            
            OnProgressUpdated.NotifyAll(BlinkProgress);
            OnBlinkStart.NotifyAll();
            //UpdateBlinkData();
            return true;
        }

        public void FinishCharge()
        {
            State = BlinkState.Finished;
        }
        
        public void CancelCharge(bool hard)
        {
            var wasCanceled = State == BlinkState.Charging && BlinkProgress < 1f;

            State = BlinkState.Idle;
            BlinkProgress = 0f;
            OnProgressUpdated.NotifyAll(BlinkProgress);

            if (wasCanceled)
                OnBlinkCanceled.NotifyAll();

            if (hard)
            {
                ClearTarget();
                _cooldown = 0f;
                _targetLost = false;
                _targetLostTime = 0f;
            }
        }
        
        public bool CanBlinkNow()
        {
            return State == BlinkState.Charged && _currentTarget && Time.time >= _cooldown;
        }

        public void ConfirmCooldown(float seconds)
        {
            _cooldown = Time.time + Mathf.Max(0f, seconds);
        }

        #endregion

        #region Target Handling

        private void DoGrace()
        {
            if (!IsValidTarget(_currentTarget) || Time.time - _targetLostTime > _config.RetainGrace)
            {
                LoseTarget();
            }
        }

        private void LoseTarget()
        {
            ClearTarget();

            if (State == BlinkState.Charging || State == BlinkState.Charged)
            {
                CancelCharge(false);
            }
        }

        private void ClearTarget()
        {
            if (!_currentTarget) return;

            _currentTarget.Set(null);
            OnTargetLost.NotifyAll();
        }

        #endregion
        
        #region Blink Data

        private bool IsValidTarget(Transform t)
        {
            if (!t) return false;
            if (!_origin.TryGet(out var origin)) return false;
            if (!_forward.TryGet(out var fwd)) return false;

            var to = t.position - origin.position;

            if (to.sqrMagnitude > _config.Range * _config.Range)
                return false;

            var dot = Vector3.Dot(fwd.forward, to.normalized);
            return dot >= _config.MinAimDot;
        }
        
        public bool TryGetBlinkCoords(out Vector3 pos, out Quaternion rot)
        {
            if (!_currentTarget.TryGet(out var targetTr) || !_origin.TryGet(out var origin))
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                return false;
            }

            var offset = _config.OffsetIsTargetLocal
                ? targetTr.TransformVector(_config.BlinkOffset)
                : _config.BlinkOffset;

            pos = targetTr.position + offset;
            
            if (_config.SnapRotationToTarget)
            {
                var dir = (targetTr.position - pos);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            
            rot = origin.rotation;
            return true;
        }

        #endregion
        
        private void HardReset()
        {
            State = BlinkState.Idle;
            BlinkProgress = 0f;
            _cooldown = 0f;

            _targetLost = false;
            _targetLostTime = 0f;

            ClearTarget();
            OnProgressUpdated.NotifyAll(BlinkProgress);
        }
        
        protected override void OnDispose()
        {
            base.OnDispose();
            
            if (_detector.TryGet(out var detector) && detector != null)
            {
                if (_onAimFound != null) detector.OnTargetFound.Detach(_onAimFound);
                if (_onAimLost != null) detector.OnTargetLost.Detach(_onAimLost);
                if (_onAimChanged != null) detector.OnTargetChanged.Detach(_onAimChanged);
            }
            
            _onAimFound?.Dispose(); 
            _onAimFound = null;
            _onAimLost?.Dispose(); 
            _onAimLost = null;
            _onAimChanged?.Dispose(); 
            _onAimChanged = null;

            _updateObserver?.Dispose();
            _updateObserver = null;

            _origin.Dispose();
            _forward.Dispose();
            _currentTarget.Dispose();
            _detector = null;
        }
        
        public override bool TryGetUpdate(out IObserver<float> observer)
        {
            _updateObserver ??= new ActionObserver<float>(Update);
            observer = _updateObserver;
            return true;
        }
        
        private void AttachDetector(TargetDetectComp detector)
        {
            if (detector == null) return;

            _onAimFound = new ActionObserver<Transform>(OnDetectorTargetFound);
            _onAimLost = new ActionObserver<Transform>(OnDetectorTargetLost);
            _onAimChanged = new ActionObserver<Transform>(OnDetectorTargetChanged);

            detector.OnTargetFound.Attach(_onAimFound);
            detector.OnTargetLost.Attach(_onAimLost);
            detector.OnTargetChanged.Attach(_onAimChanged);

            // If detector already has a target when we spawn, treat as found
            if (detector.CurrentTarget.TryGet(out var t) && t != null)
            {
                OnDetectorTargetFound(t);
            }
        }
    }
}