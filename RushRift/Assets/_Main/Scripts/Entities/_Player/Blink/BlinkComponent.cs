using Game.DesignPatterns.Observers;
using Game.Utils;
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
        private NullCheck<EnergyComponent> _energyComp;

        private NullCheck<Transform> _newTarget;
        private NullCheck<Transform> _currentTarget;
        private float _cooldown;

        private ActionObserver<float> _updateObserver;
        
        // detector observers
        private ActionObserver<Transform> _onAimFound;
        private ActionObserver<Transform> _onAimLost;
        private ActionObserver<Transform> _onAimChanged;

        private bool _targetOutOfSight;
        private float _graceTimer;
        private int _executedCount;

        public BlinkComponent(BlinkConfig config, TargetDetectComp detector, Transform origin, Transform forward, EnergyComponent energyComp)
        {
            _config = config;
            _detector = detector;
            _origin = origin;
            _forward = forward;
            _energyComp = energyComp;

            OnLoadingObserver = new NullCheck<ActionObserver<bool>>(new ActionObserver<bool>(OnLoadingHandler));

            AttachDetector(detector);
        }

        public BlinkComponent(BlinkConfig config, TargetDetectComp detector, EnergyComponent energyComp = null) : this(config, detector, detector.Origin,
            detector.Forward, energyComp) { }
        
        private void Update(float delta)
        {
            if (!_origin || !_forward || !GlobalLevelManager.Blink) return;

            if (_newTarget.TryGet(out var newT) && !newT.IsNullOrMissing() && InRange(newT))
            {
                StopGrace();
                SetTarget(newT);
                _newTarget.Set(null);
            }
            
            if (!_currentTarget.TryGet(out var t)) return;

            // If it is invalid, destroyed, disabled, pooled, etc.
            if (t.IsNullOrMissing() || !t.gameObject.activeInHierarchy)
            {
                StopCharge();
                return;
            }
            
            
            // if target is to far away, lose the lock
            if (!InRange(t, _config.RangeOffset))
            {
                StopCharge();
                return;
            }

            if (_targetOutOfSight)
            {
                _graceTimer -= delta;
                if (_graceTimer <= 0 || !IsInFOV(t))
                {
                    StopCharge();
                    return;
                }
            }
            
            
            if (State == BlinkState.Charging)
            {
                if (!_currentTarget)
                {
                    CancelCharge(hard: false);
                    return;
                }

                SetProgress(BlinkProgress + delta / Mathf.Max(0.0001f, _config.LockTime));
                
                if (BlinkProgress >= 1f)
                {
                    State = BlinkState.Charged;
                    
                    SetProgress(1f);
                    OnBlinkEnd.NotifyAll(); // finished charging
                    return;
                }
                
            }
        }

        private void OnLoadingHandler(bool state)
        {
            this.Log("Reset Blink On Load");
            HardReset();
            _executedCount = 0;
        }

        #region Grace Methods

        private bool InRange(Transform t, float offset = 0)
        {
            if (!t) return false;
            if (!_origin.TryGet(out var origin)) return false;

            var hasEnergy = _energyComp.TryGet(out var energy);

            var range = _config.Range;
            var finalRange = !hasEnergy
                ? range + offset
                : (range + (((_config.RangeBoost * (energy.Value - 1)) / 100) * 70)) + offset;
            
            //var range = (_config.Range + ((_config.RangeBoost / 100) * 70) * currEnergy) + offset;
            //var range = _config.Range + offset;
            var to = t.position - origin.position;
            return to.sqrMagnitude <= finalRange * finalRange;
        }
        
        private void StopGrace()
        {
            _targetOutOfSight = false;
            _graceTimer = 0;
        }

        private void StartGrace()
        {
            _targetOutOfSight = true;
            _graceTimer = _config.LoseDelay;
        }

        #endregion
        

        #region Detector Callbacks

        private void OnDetectorTargetFound(Transform t)
        {
            if (!GlobalLevelManager.Blink) return;
            if (!InRange(t))
            {
                _newTarget.Set(t);
                return;
            }
            
            StopGrace();

            if (_currentTarget.TryGet(out var curr) && curr == t) return;
            SetTarget(t);
        }

        private void OnDetectorTargetChanged(Transform t)
        {
            if (!GlobalLevelManager.Blink || !InRange(t)) return;
            StopGrace();

            SetTarget(t);
        }
        
        private void OnDetectorTargetLost(Transform t)
        {
            if (!GlobalLevelManager.Blink) return;

            if (!_currentTarget.TryGet(out var curr) || curr.IsNullOrMissing() || !curr.gameObject.activeInHierarchy)
            {
                StopCharge();
                return;
            }
            
            
            StartGrace();
        }

        #endregion

        #region Charge Control

        public bool BeginCharge()
        {
            if (State != BlinkState.Idle) return false;
            if (Time.time < _cooldown) return false;
            if (!_currentTarget) return false;

            State = BlinkState.Charging;
            SetProgress(0f);
            OnBlinkStart.NotifyAll();
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
            SetProgress(0f);

            if (wasCanceled)
                OnBlinkCanceled.NotifyAll();

            if (hard)
            {
                ClearTarget();
                _cooldown = 0f;
                _targetOutOfSight = false;
                _graceTimer = 0;
            }
        }

        private void StopCharge()
        {
            State = BlinkState.Idle;
            SetProgress(0f);
            
            ClearTarget();
            _cooldown = 0f;
            _targetOutOfSight = false;
            _graceTimer = 0;
        }

        private void SetProgress(float progress)
        {
            BlinkProgress = Mathf.Clamp01(progress);
            OnProgressUpdated.NotifyAll(progress);
        }

        public void ConfirmCooldown(float seconds)
        {
            _cooldown = Time.time + Mathf.Max(0f, seconds);
        }

        #endregion

        #region Target Handling
        
        private void ClearTarget()
        {
            if (!_currentTarget) return;

            _currentTarget.Set(null);
            OnTargetLost.NotifyAll();
        }
        
        private void SetTarget(Transform t)
        {
            if (_currentTarget.TryGet(out var curr) && curr != null)
            {
                if (curr == t) return;
                _currentTarget.Set(t);
                OnTargetChanged.NotifyAll();
            }
            else
            {
                _currentTarget.Set(t);
                OnTargetFound.NotifyAll();
            }
        }

        #endregion
        
        #region Blink Data

        private bool IsInFOV(Transform t)
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
            if (!_currentTarget.TryGet(out var targetTr) || !_origin.TryGet(out var origin) || 
                targetTr.IsNullOrMissing() || !targetTr.gameObject.activeInHierarchy)
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                return false;
            }

            var offset = _config.OffsetIsTargetLocal
                ? targetTr.TransformVector(_config.BlinkOffset)
                : _config.BlinkOffset;

            pos = targetTr.position + offset;
            rot = origin.rotation;
            
            if (_config.SnapRotationToTarget)
            {
                var dir = (targetTr.position - pos);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            
            return true;
        }

        #endregion
        
        private void HardReset()
        {
            State = BlinkState.Idle;
            _cooldown = 0f;

            
            _targetOutOfSight = false;
            _graceTimer = 0;

            ClearTarget();
            
            SetProgress(0f);
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

        public int ExecutedCount()
        {
            return _executedCount;
        }

        public void IncreaseExecutedCount()
        {
            _executedCount++;
        }
    }
}