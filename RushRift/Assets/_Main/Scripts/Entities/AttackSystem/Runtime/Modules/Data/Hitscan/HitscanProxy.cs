using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.LevelElements;
using Object = UnityEngine.Object;

namespace Game.Entities.AttackSystem.Hitscan
{
    public class HitscanProxy : ModuleProxy<HitscanModule>
    {
        private bool _executed;
        private float _timer;
        private Action _lateUpdate = delegate {};
        
        // Debug variables
        private Vector3 _startPos;
        private Vector3 _direction;
        private bool _detected;

        struct HitData
        {
            public float distance;
            public Vector3 point;
            public Collider collider;
            public HitType type;
            public float weight;
            public float dot;
        }
        
        public HitscanProxy(HitscanModule data, IModuleProxy[] children, bool disposeData = false) : base(data, children, disposeData)
        {
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnStart);
            LateUpdateObserver = new ActionObserver<ModuleParams, float>(OnLateUpdate);
        }

        private void OnStart(ModuleParams mParams)
        {
            _executed = false;
            _timer = 0;
        }
        
        private void OnLateUpdate(ModuleParams mParams, float delta)
        {
            if (_executed) return;
            _timer += delta;

            if (_timer >= Data.Delay)
            {
                _executed = true;
                Shoot(mParams, delta);
                
            }

            _lateUpdate();
        }


        private Vector3 CalculateSpawnPosition(Vector3 spawnPos, Vector3 velocity, Vector3 forward, float time, float delta)
        {
            var compensatedPosition = spawnPos + velocity * delta;

            return compensatedPosition;
        }

        private void Shoot(ModuleParams mParams, float delta)
        {
            if (Data.Muzzle) Data.Muzzle.Play();

            var origin = mParams.Joints.GetJoint(Data.OriginJoint);
            var spawn = mParams.Joints.GetJoint(Data.SpawnJoint);
            
            var spawnPos = Data.GetOffsetPosition(spawn);
            var direction = Data.GetDirection(origin.position, origin.forward, spawnPos);
            
            
            if (!mParams.Owner) return;
            var trail = Object.Instantiate(Data.Line, spawnPos, Quaternion.identity);
            trail.Enable(false);
            _startPos = spawnPos;
            _direction = direction;

            var hits = new List<HitData>();

            var worldHits = Physics.RaycastAll(spawnPos, direction, Data.Range, Data.GroundMask);
            foreach (var h in worldHits)
            {
                if (h.collider == null) continue;

                var toHit = (h.point - spawnPos).normalized;
                var dot = Vector3.Dot(direction, toHit);
                
                if (Data.UseDotThreshold && dot < Data.MinDotThreshold) continue;
                
                hits.Add(new HitData()
                {
                    distance = h.distance,
                    point = h.point,
                    collider = h.collider,
                    type = HitType.World,
                    weight = Data.Weights.TryGetValue(HitType.World, out var w) ? w : 0f,
                    dot = dot
                });
            }

            var enemyHits = Physics.SphereCastAll(spawnPos, Data.Radius, direction, Data.Range, Data.EntityMask);
            foreach (var h in enemyHits)
            {
                if (h.collider == null) continue;
                
                var type = DetermineEntityHitType(h.collider, Data);

                var toHit = (h.point - spawnPos).normalized;
                float dot = Vector3.Dot(direction, toHit);

                if (Data.UseDotThreshold && dot < Data.MinDotThreshold) continue;

                hits.Add(new HitData()
                {
                    distance = h.distance,
                    point = h.point,
                    collider = h.collider,
                    type = type,
                    weight = Data.Weights.TryGetValue(type, out var w) ? w : 0f,
                    dot = dot
                });
            }

            if (hits.Count == 0)
            {
                // No hits
                _detected = false;
                trail.SetPosition(spawn, spawnPos + (direction * Data.Range), Data.LineDuration, Data.Offset);
                
                if (Data.UseSFX) AudioManager.Play(Data.SFXName);
                trail.Enable(true);
                return;
            }

            // Sort by distance and weight
            hits.Sort((a, b) =>
            {
                var distCompare = a.distance.CompareTo(b.distance);
                if (distCompare != 0)
                    return distCompare;

                return b.weight.CompareTo(a.weight);
            });
            
            var hit = hits[0];
            
            _detected = true;
            trail.SetPosition(spawn, hit.point, Data.LineDuration, Data.Offset);

            switch (hit.type)
            {
                case HitType.Entity:
                    if (hit.collider.TryGetComponent<IController>(out var ctrl) &&
                        ctrl.GetModel().TryGetComponent<HealthComponent>(out var hp))
                        hp.Damage(Data.Damage, spawnPos);
                    break;
                case HitType.Projectile:
                    if (hit.collider.TryGetComponent<Projectile>(out var p))
                    {
                        if (Data.ChainReaction)
                        {
                            p.TriggerHitByHitscan(spawnPos, direction, Data.ChainRadius, Data.ChainDamage, Data.ChainLayer, Data.Line);
                        }
                        else
                        {
                            p.DestroyProjectile();
                        }
                    }
                    break;
                case HitType.Terminal:
                    if (hit.collider.TryGetComponent<Terminal>(out var terminal))
                        terminal.Do();
                    break;
            }
            
            EffectManager.TryGetVFX(Data.ImpactID, new VFXEmitterParams()
            {
                scale = Data.ImpactSize,
                position = hit.point,
                rotation = Quaternion.identity,
            }, out var emitter);
            
            if (Data.UseSFX) AudioManager.Play(Data.SFXName);
            trail.Enable(true);
        }
        
        private HitType DetermineEntityHitType(Collider col, HitscanModule Data)
        {
            if (col.TryGetComponent<Projectile>(out _))
                return HitType.Projectile;

            if (col.gameObject.layer == 12 && Data.CanUseTerminals)
                return HitType.Terminal;

            return HitType.Entity;
        }

        public override void OnDraw(Transform origin)
        {
            Gizmos.color = _detected ? Color.green : Color.red;
            Gizmos.DrawRay(_startPos, _direction * Data.Range);
        }

        private Collider[] _colliders = new Collider[3];
        private bool HitEntity(Vector3 spawnPos, Vector3 direction, float handRadius, out Vector3 closestPoint, out Collider collider)
        {
            if (Physics.SphereCast(spawnPos, handRadius, direction, out var hit, Data.Range, Data.EntityMask))
            {
                closestPoint = hit.point;
                collider = hit.collider;
                return true;
            }

            //if (Physics.OverlapSphere(spawnPos, Data.Radius, Data.EntityMask) > 0)
            if (Physics.OverlapSphereNonAlloc(spawnPos, handRadius, _colliders, Data.EntityMask) > 0)
            {
                collider = _colliders.FirstOrDefault();

                if (collider == null)
                {
                    Debug.LogError("ERROR: The collider in HitScan proxy is null");
                    closestPoint = Vector3.zero;
                }
                else
                {
                    closestPoint = collider.ClosestPoint(spawnPos);
                }
                
                return true;
            }

            collider = null;
            closestPoint = Vector3.zero;
            return false;
        }
    }
}