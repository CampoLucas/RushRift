using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    /// <summary>
    /// Constantly detects the target the player is aiming at.
    /// Has events for when it founds, looses and changes target.
    /// </summary>
    public class TargetDetectComp : EntityComponent
    {
        public NullCheck<Transform> CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget;
        public Transform Origin => _origin.Get();
        public Transform Forward => _forward.Get();

        public Subject<Transform> OnTargetFound { get; } = new();
        public Subject<Transform> OnTargetLost { get; } = new();
        public Subject<Transform> OnTargetChanged { get; } = new();

        private readonly TargetDetectConfig _config;

        private NullCheck<Transform> _currentTarget;
        private NullCheck<Transform> _forward;
        private NullCheck<Transform> _origin;

        private RaycastHit[] _hits;
        private Collider[] _overlaps;
        private ActionObserver<float> _updateObserver;
        private ActionObserver<bool> _onLoadingObserver;

        public TargetDetectComp(TargetDetectConfig config, Transform origin, Transform forward)
        {
            _config = config;
            _origin = origin;
            _forward = forward;

            _hits = new RaycastHit[_config.MaxHits];
            _overlaps = new Collider[_config.MaxHits];

            OnLoadingObserver = new NullCheck<ActionObserver<bool>>(new ActionObserver<bool>(OnLoadingHandler));
        }

        private void OnLoadingHandler(bool state)
        {
            _currentTarget = new NullCheck<Transform>();
            Array.Clear(_hits, 0, _hits.Length);
            Array.Clear(_overlaps, 0, _overlaps.Length);
        }

        private void Update(float delta)
        {
            if (!_origin || !_forward)
                return;

            //var foundTarget = FindTarget();
            var hasTarget = _currentTarget.TryGet(out var curr);

            if (!FindTarget().TryGet(out var foundTarget))
            {
                if (!hasTarget) return;
                _currentTarget.Set(null);
                OnTargetLost.NotifyAll(curr);
                return;
            }

            if (!hasTarget)
            {
                _currentTarget.Set(foundTarget);
                OnTargetFound.NotifyAll(foundTarget);
                return;
            }

            if (curr == foundTarget) return;
            _currentTarget.Set(foundTarget);
            OnTargetChanged.NotifyAll(foundTarget);
        }

        private NullCheck<Transform> FindTarget()
        {
            var result = new NullCheck<Transform>();

            if (!_forward.TryGet(out var fwd)) return result;

            var origin = fwd.position;
            
            Transform best = null;
            var bestDist = float.PositiveInfinity;
            
            // First overlap
            var overlapCount = Physics.OverlapSphereNonAlloc(origin, _config.SphereRadius, _overlaps,
                _config.IncludeLayers, _config.TriggerInteraction);
            
            if (overlapCount > 0)
            {
                for (var i = 0; i < overlapCount; i++)
                {
                    var col = _overlaps[i];
                    if (!col) continue;

                    var targetPoint = col.ClosestPoint(origin);
                    var dist = (targetPoint - origin).magnitude;

                    if (dist > _config.Range) continue;

                    TryPickBest(origin, col, targetPoint, dist, ref best, ref bestDist);
                }
                
                if (best != null)
                {
                    result.Set(best);
                }
            }
            else
            {
                var ray = new Ray(origin, fwd.forward);

                var hitCount = Physics.SphereCastNonAlloc(ray, _config.SphereRadius, _hits, _config.Range,
                    _config.IncludeLayers, _config.TriggerInteraction);

                if (hitCount <= 0) return result;
                for (var i = 0; i < hitCount; i++)
                {
                    var hit = _hits[i];
                    if (!hit.collider) continue;

                    TryPickBest(origin, hit.collider, hit.point, hit.distance, ref best, ref bestDist);
                }

                if (best != null)
                    result.Set(best);
                
                
                // var dirForward = fwd.forward;
                // var ray = new Ray(origin, dirForward);
                //
                // var hitCount = Physics.SphereCastNonAlloc(ray, _config.SphereRadius, _hits, _config.Range,
                //     _config.IncludeLayers, _config.TriggerInteraction);
                //
                // if (hitCount <= 0) return result;
                // best = GetBest(origin, bestDist, hitCount, best);
            }
            
            if (best != null)
                result.Set(best);

            return result;
        }

        private void TryPickBest(Vector3 origin, Collider col, Vector3 targetPoint, float targetDist, ref Transform best,
            ref float bestDist)
        {
            // Obstacle check: obstacle must be closer than target to block
            if (_config.IgnoreLayers.value != 0)
            {
                var dir = targetPoint - origin;
                var dist = dir.magnitude;

                if (dist > 0.0001f)
                {
                    dir /= dist;

                    if (Physics.Raycast(origin, dir, out var obstacleHit, dist, _config.IgnoreLayers,
                            QueryTriggerInteraction.Ignore))
                    {
                        // obstacle is in front of target
                        return;
                    }
                }
            }

            // Resolve target transform
            var tr = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;

            if (!tr || !(targetDist < bestDist)) return;

            bestDist = targetDist;
            best = tr;
        }

        private void HardReset()
        {
            if (_currentTarget)
            {
                _currentTarget.Set(null);
                OnTargetLost.NotifyAll(null);
            }
        }

        public override bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            _updateObserver ??= new ActionObserver<float>(Update);
            observer = _updateObserver;
            return true;
        }
        
        protected override void OnDispose()
        {
            base.OnDispose();

            _updateObserver?.Dispose();
            _updateObserver = null;

            _currentTarget.Dispose();
            _origin.Dispose();
            _forward.Dispose();

            _hits = null;
            
            OnTargetFound?.Dispose();
            OnTargetLost?.Dispose();
            OnTargetChanged?.Dispose();
        }

    }
}