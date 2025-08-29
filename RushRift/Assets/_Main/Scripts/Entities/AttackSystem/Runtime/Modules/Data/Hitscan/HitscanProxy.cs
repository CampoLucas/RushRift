using System.Collections;
using Game.DesignPatterns.Observers;
using Game.DesignPatterns.Pool;
using Game.Entities.Components;
using UnityEngine;
using System;
using System.Linq;
using Game.Detection;
using Game.LevelElements.Terminal;
using Unity.VisualScripting;
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
            
            //direction += movement.Velocity;
            //spawnPos = CalculateSpawnPosition(spawnPos, movement.Velocity, movement.Velocity, movement.Velocity.magnitude, delta);
            

            
            var trail = Object.Instantiate(Data.Line, spawnPos, Quaternion.identity);
            trail.Enable(false);
            
            //trail.SetDuration(Data.LineDuration);

            _startPos = spawnPos;
            _direction = direction;
            
#if false
            if (Physics.Raycast(spawnPos, direction, out var hit, Data.Range, Data.EntityMask))
#else
            if (HitEntity(spawnPos, direction, Data.Radius, out var point, out var collider)) // Checks if it collided with an entity
#endif
            {
                _detected = true;
                trail.SetPosition(spawn, point, Data.LineDuration, Data.Offset);
                var other = collider.gameObject;
                if (other.TryGetComponent<IController>(out var controller) &&
                    controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
                {
                    healthComponent.Damage(Data.Damage, spawnPos);
                }
                else if (LevelManager.CanUseTerminal && other.layer == 12 && other.TryGetComponent<Terminal>(out var terminal))
                {
                    terminal.Do();
                }

                LevelManager.TryGetVFX(Data.ImpactID, new VFXEmitterParams()
                {
                    scale = Data.ImpactSize,
                    position = point,
                    rotation = Quaternion.identity,
                }, out var emitter);

            }
            else if (Physics.Raycast(spawnPos, direction, out var hit, Data.Range, Data.GroundMask)) // Checks if it collided with the ground
            {
                _detected = true;
                trail.SetPosition(spawn, hit.point, Data.LineDuration, Data.Offset);
                // Play particles when collided with the ground
                
                // LevelManager.TryGetVFX(Data.ImpactID, new VFXEmitterParams()
                // {
                //     scale = Data.ImpactSize,
                //     position = hit.point,
                //     rotation = Quaternion.identity,
                // }, out var emitter);

                LevelManager.TryGetVFX(Data.ImpactID, new VFXEmitterParams()
                {
                    scale = Data.ImpactSize,
                    position = hit.point,
                    rotation = Quaternion.identity,
                }, out var emitter);
            }
            else
            {
                _detected = false;
                trail.SetPosition(spawn, spawnPos + (direction * Data.Range), Data.LineDuration, Data.Offset);
            }
            
            if (Data.UseSFX) AudioManager.Play(Data.SFXName);
            trail.Enable(true);

            
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