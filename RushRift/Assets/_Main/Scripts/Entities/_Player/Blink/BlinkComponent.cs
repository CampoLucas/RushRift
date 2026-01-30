using Game.DesignPatterns.Observers;
using Game.Levels;
using MyTools.Global;
using UnityEngine;

namespace Game.Entities.Components
{
    public class BlinkComponent : EntityComponent
    {
        public NullCheck<Transform> CurrentTarget => _currentTarget;
        private NullCheck<Transform> _currentTarget;
        public bool HasTarget => _currentTarget;
        public float LockProgress { get; private set; }
        public bool IsCharging { get; private set; }
        public bool IsReady => LockProgress >= 1f;
        public bool BlinkFinished { get; private set; }
        
        public Subject OnTargetFound { get; private set; } = new();
        public Subject OnTargetChanged { get; private set; } = new();
        public Subject OnTargetLost { get; private set; } = new();
        public Subject<float> OnProgressUpdated { get; private set; } = new();
        public Subject OnBlinkStart { get; private set; } = new();
        public Subject OnBlinkEnd { get; private set; } = new();
        
        private readonly BlinkConfig _config;

        private NullCheck<Transform> _origin;
        private NullCheck<Transform> _forward;
        private NullCheck<Rigidbody> _rb;
        private NullCheck<Transform> _aimTarget;

        private float _lastSeenTime;
        private float _cooldown;

        private RaycastHit[] _hits;
        private ActionObserver<float> _updateObserver;
        
        public BlinkComponent(BlinkConfig config, Transform origin, Transform forward, Rigidbody rb = null)
        {
            _config = config;
            _origin = origin;
            _forward = forward;
            _rb = rb;

            _hits = new RaycastHit[_config.MaxHits];

            OnLoading.Set(new ActionObserver<bool>(OnLoadingHandler));
        }
        
        private void Update(float delta)
        {
            if (!_origin || !_forward)
            {
                this.Log("returning");
                return;
            }

            UpdateTarget();
            
            if (IsCharging && HasTarget && Time.time >= _cooldown)
            {
                this.Log($"Lock progress ongoing LP: {LockProgress} Cooldown: {_cooldown}");
                LockProgress += delta / _config.LockTime;
                if (LockProgress > 1f) LockProgress = 1f;
            }
            else
            {
                LockProgress = 0f;
            }
            OnProgressUpdated.NotifyAll(LockProgress);
        }

        private void OnLoadingHandler(bool state)
        {
            ResetState(true);
        }
        
        public override bool TryGetUpdate(out IObserver<float> observer)
        {
            _updateObserver ??= new ActionObserver<float>(Update);
            observer = _updateObserver;
            return true;
        }

        // public void SetCharging(bool charging)
        // {
        //     if (!IsCharging && charging)
        //     {
        //         BlinkFinished = false;
        //     }
        //     
        //     if (IsCharging && !charging)
        //     {
        //         LockProgress = 0f;
        //     }
        //
        //     IsCharging = charging;
        // }

        public bool CanBlink()
        {
            if (!_origin || !_forward) return false;
            if (Time.time < _cooldown) return false;

            if (!HasTarget)
            {
                this.Log("It doesn't have a target");
            }

            if (!IsReady)
            {
                this.Log("It is not ready");
            }
            
            return HasTarget && IsReady;
        }

        public bool TryAutoBlink()
        {
            if (!IsCharging) return false;
            return TryBlink();
        }

        public bool TryBlink(bool forced = false)
        {
            if (!forced && !CanBlink()) return false;
            return ExecuteBlink(_currentTarget);
        }

        private void UpdateTarget()
        {
            var aimed = GetTarget();
            
            if (aimed && !_aimTarget)
                OnTargetFound.NotifyAll();
            
            if (!aimed && _aimTarget)
                OnTargetLost.NotifyAll();

            _aimTarget = aimed;
            
            if (aimed.TryGet(out var t))
            {
                if (_currentTarget.TryGet(out var curr) && curr != t)
                {
                    OnTargetChanged.NotifyAll();
                }
                
                _currentTarget.Set(t);
                _lastSeenTime = Time.time;
                return;
            }
            
            // if t is null apply grace
            if (!_currentTarget) return;

            if (!IsCharging)
            {
                _currentTarget.Set(null);
                return;
            }

            if (Time.time - _lastSeenTime > _config.RetainGrace)
            {
                _currentTarget.Set(null);
            }
        }

        private NullCheck<Transform> GetTarget()
        {
            // Ray from the camera to the center
            var forward = _forward.Get();
            var ray = new Ray(forward.position, forward.forward);

            var hitCount = Physics.SphereCastNonAlloc(
                ray,
                _config.SphereRadius,
                _hits,
                GetRange(),
                _config.TargetLayers,
                QueryTriggerInteraction.Ignore
            );

            if (hitCount <= 0) return null;
            
            // Pick closest valid
            Transform best = null;
            var bestDist = float.PositiveInfinity;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                var tr = hit.collider ? hit.collider.transform : null;
                if (tr == null) continue;

                // Tag filter
                if (!string.IsNullOrEmpty(_config.RequiredTag) && !tr.CompareTag(_config.RequiredTag))
                {
                    // If collider is on a child, the tag might be on the root.
                    var root = tr.root;
                    if (root == null || !root.CompareTag(_config.RequiredTag))
                        continue;

                    tr = root;
                }

                // LOS check
                if (_config.RequireLineOfSight)
                {
                    var targetPos = tr.position;
                    var dir = (targetPos - forward.position);
                    var dist = dir.magnitude;
                    if (dist > 0.0001f)
                    {
                        if (Physics.Raycast(forward.position, dir / dist, out var losHit, dist, ~0, QueryTriggerInteraction.Ignore))
                        {
                            // If something else blocks, reject
                            if (losHit.collider == null || losHit.collider.transform.root != tr.root)
                                continue;
                        }
                    }
                }

                var d = hit.distance;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = tr;
                }
            }

            return best;
        }
        
        private bool ExecuteBlink(NullCheck<Transform> target)
        {
            if (!target.TryGet(out var targetTr) || !_origin.TryGet(out var origin)) return false;

            var offset = _config.OffsetIsTargetLocal
                ? targetTr.TransformVector(_config.BlinkOffset)
                : _config.BlinkOffset;

            var blinkPos = targetTr.position + offset;

            origin.position = blinkPos;

            if (_config.SnapRotationToTarget)
            {
                var dir = (targetTr.position - origin.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    origin.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            if (_config.ZeroVelocity && _rb.TryGet(out var rb))
                rb.velocity = Vector3.zero;

            _cooldown = Time.time + _config.Cooldown;
            
            if (_config.KillOnBlink)
                KillTarget(targetTr);
            
            EndBlink();
            
            return true;
        }

        public void BeginBlink()
        {
            OnBlinkStart.NotifyAll();
            BlinkFinished = false;
            LockProgress = 0f;
            IsCharging = true;
            //ResetState(hard: false);
        }
        
        private void EndBlink()
        {
            OnBlinkEnd.NotifyAll();
            BlinkFinished = true;
            ResetState(hard: false); // keep target if you want, but clear charge
        }
        
        public void ResetState(bool hard)
        {
            
            LockProgress = 0f;
            IsCharging = false;

            if (hard)
            {
                _currentTarget.Set(null);
                _cooldown = 0f;
                _lastSeenTime = 0f;
                BlinkFinished = false;
            }
        }
        
        private void KillTarget(NullCheck<Transform> t)
        {
            if (!t.TryGet(out var target)) return;

            // var controller = target.GetComponentInParent<EntityController>();
            // if (controller == null) return;
            
            var controller = target.GetComponentInParent<EntityController>();
            var barrel = target.GetComponentInParent<ExplosiveBarrel>();
            if (barrel && _rb.TryGet(out var rb)) barrel.TriggerExplosionExternal(null, true, rb);

            if (controller != null)
            {
                var model = controller.GetModel();
                if (model != null && model.TryGetComponent<HealthComponent>(out var health))
                {
                    health.Intakill(target.position);
                    return;
                }
                controller.OnNotify(EntityController.DESTROY);
                
            }

        }

        private float GetRange() => _config.Range;

        protected override void OnDispose()
        {
            base.OnDispose();
            _updateObserver?.Dispose();
            _updateObserver = null;
            _hits = null;
            _currentTarget.Dispose();
            _origin.Dispose();
            _forward.Dispose();
            _rb.Dispose();
        }
    }
}