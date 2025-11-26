using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.LevelElements;
using MyTools.Global;
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
            public object detected;
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

        private void Shoot(ModuleParams mParams, float delta)
        {
            if (!mParams.Owner) return;
            if (Data.Muzzle) Data.Muzzle.Play();

            GetSpawnParameters(mParams, out var origin, out var spawn, out var spawnPos, out var direction);
            
            var trail = Object.Instantiate(Data.Line, spawnPos, Quaternion.identity);
            trail.Enable(false);
            _startPos = spawnPos;
            _direction = direction;

            var hits = new List<RaycastHit>();
            
            var worldHits = Physics.RaycastAll(spawnPos, direction, Data.Range, Data.GroundMask);
            var overlapHits = Physics.OverlapSphere(spawnPos, Data.Radius, Data.EntityMask);
            var enemyHits = Physics.SphereCastAll(spawnPos, Data.Radius, direction, Data.Range, Data.EntityMask);
            hits.AddRange(worldHits);
            hits.AddRange(enemyHits);
                
            var hitDatas = new List<HitData>();
            
            
            foreach (var col in overlapHits)
            {
                if (col == null) continue;

                var type = DetermineHitType(col, Data, out var detected);

                // Compute a fake hit point: closest point to the muzzle
                var closestPoint = spawnPos;

                var dot = 1;

                if (Data.UseDotThreshold && dot < Data.MinDotThreshold)
                    continue;

                hitDatas.Add(new HitData()
                {
                    distance = Vector3.Distance(spawnPos, closestPoint),
                    point = closestPoint,
                    collider = col,
                    type = type,
                    weight = Data.Weights.TryGetValue(type, out var w) ? w : 0f,
                    dot = dot,
                    detected = detected
                });
            }

            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                
                // If the sphere cast already collected this collider, skip duplicate
                if (overlapHits.Any(coll => coll == h.collider)) 
                    continue;
                
                var type = DetermineHitType(h.collider, Data, out var d);
                var toHit = (h.point - spawnPos).normalized;
                var dot = Vector3.Dot(direction, toHit);
                
                if (Data.UseDotThreshold && dot < Data.MinDotThreshold) continue;

                hitDatas.Add(new HitData()
                {
                    distance = Vector3.Distance(spawnPos, h.point),
                    point = h.point,
                    collider = h.collider,
                    type = type,
                    weight = Data.Weights.TryGetValue(type, out var w) ? w : 0f,
                    dot = dot,
                    detected = d
                });
            }
            
            if (hitDatas.Count == 0)
            {
                // No hits
                _detected = false;
                trail.SetPosition(spawn, spawnPos + (direction * Data.Range), Data.LineDuration, Data.Offset);
                
                if (Data.UseSFX) AudioManager.Play(Data.SFXName);
                trail.Enable(true);
                return;
            }

            // Sort by distance and weight
            hitDatas.Sort((a, b) =>
            {
                // Distance is primary
                var distCompare = a.distance.CompareTo(b.distance);
                if (distCompare != 0)
                    return distCompare;
                
                // Dot if the flag is on
                if (Data.UseDotThreshold)
                {
                    var dotCompare = b.dot.CompareTo(a.dot);
                    if (dotCompare != 0)
                        return dotCompare;
                }

                // Fallback with the weight
                return b.weight.CompareTo(a.weight);
            });
            
            var hit = hitDatas[0];
            
            _detected = true;
            trail.SetPosition(spawn, hit.point, Data.LineDuration, Data.Offset);

            switch (hit.type)
            {
                case HitType.Entity:
                    if (hit.detected is IController controller)
                    {
                        OnEntityHit(controller, spawnPos);
                    }
                    break;
                case HitType.Projectile:
                    if (hit.detected is Projectile projectile)
                    {
                        OnProjectileHit(projectile, spawnPos, direction);
                    }
                    break;
                case HitType.Terminal:
                    if (hit.detected is Terminal terminal)
                    {
                        OnTerminalHit(terminal, hit, spawnPos);
                    }
                    break;
                case HitType.World:
                    if (hit.detected is Collider collider)
                    {
                        OnWorldHit(collider);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

        private void GetSpawnParameters(in ModuleParams mParams, out Transform origin, out Transform spawn,
            out Vector3 spawnPosition, out Vector3 direction)
        {
            origin = mParams.Joints.GetJoint(Data.OriginJoint);
            spawn = mParams.Joints.GetJoint(Data.SpawnJoint);
            spawnPosition = Data.GetOffsetPosition(spawn);
            direction = Data.GetDirection(origin.position, origin.forward, spawnPosition);

        }
        
        private HitType DetermineHitType(Collider col, HitscanModule Data, out object detected)
        {
            if (col.TryGetComponent<Projectile>(out var projectile))
            {
                detected = projectile;
                return HitType.Projectile;
            }

            if (Data.CanUseTerminals && GlobalLevelManager.PowerSurge && col.gameObject.layer == 12 && col.gameObject.TryGetComponent<Terminal>(out var terminal))
            {
                detected = terminal;
                return HitType.Terminal;
            }

            if (col.TryGetComponent<IController>(out var entity))
            {
                detected = entity;
                return HitType.Entity;
            }

            detected = col;
            return HitType.World;
        }

        private void OnEntityHit(in IController ctrl, in Vector3 spawnPos)
        {
            if (ctrl.GetModel().TryGetComponent<HealthComponent>(out var hp))
                hp.Damage(Data.Damage, spawnPos);
        }

        private void OnProjectileHit(in Projectile projectile, in Vector3 spawnPos, in Vector3 direction)
        {
            if (Data.ChainReaction)
            {
                projectile.TriggerHitByHitscan(spawnPos, direction, Data.ChainRadius, Data.ChainDamage, Data.ChainLayer, Data.Line, Data.ChainLifetime);
            }
            else
            {
                projectile.DestroyProjectile();
            }
        }

        private void OnTerminalHit(in Terminal terminal, in HitData hit, in Vector3 spawnPos)
        {
            terminal.Do();
        }

        private void OnWorldHit(in Collider collider)
        {
            
        }

        public override void OnDraw(Transform origin)
        {
            Gizmos.color = _detected ? Color.green : Color.red;
            Gizmos.DrawRay(_startPos, _direction * Data.Range);
        }
    }
}