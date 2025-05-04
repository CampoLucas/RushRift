using System.Collections;
using Game.DesignPatterns.Observers;
using Game.DesignPatterns.Pool;
using Game.Entities.Components;
using UnityEngine;
using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Game.Entities.AttackSystem.Hitscan
{
    public class HitscanProxy : ModuleProxy<HitscanModule>
    {
        private bool _executed;
        private float _timer;
        private Action _lateUpdate = delegate {};
        
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
                Debug.Log($"Fired timer {_timer} {Timer}");
                _executed = true;
                Shoot(mParams, delta);
                
            }

            _lateUpdate();
        }


        private Vector3 CalculateSpawnPosition(Vector3 spawnPos, Vector3 velocity, Vector3 forward, float time, float delta)
        {
            Debug.Log($"Velocity is {velocity}, speed mag {velocity.magnitude}");
            var compensatedPosition = spawnPos + velocity * delta;

            return compensatedPosition;
        }

        private void Shoot(ModuleParams mParams, float delta)
        {
            if (Data.Muzzle) Data.Muzzle.Play();
            var spawnPos = Data.GetOffsetPosition(mParams.OriginTransform);

            var direction = Data.GetDirection(mParams.EyesTransform.position, mParams.EyesTransform.forward, spawnPos);
            
            
            if (!mParams.Owner || !mParams.Owner.Get().GetModel().TryGetComponent<IMovement>(out var movement)) return;
            
            //direction += movement.Velocity;
            //spawnPos = CalculateSpawnPosition(spawnPos, movement.Velocity, movement.Velocity, movement.Velocity.magnitude, delta);
            

            
            var trail = Object.Instantiate(Data.Trail, Vector3.zero, Quaternion.identity);
            
            if (Physics.Raycast(spawnPos, direction, out var hit, Data.Range,
                    Data.Mask))
            {
                if (Data.Trail)
                {
                    if (mParams.Owner)
                    {
                        //mParams.Owner.Get().DoCoroutine(SpawnTrail(trail, mParams.OriginTransform, hit.point, hit.normal, movement));
                        //mParams.Owner.Get().DoCoroutine(SpawnTrail(trail, mParams.OriginTransform, hit.point, movement));
                        trail.SetPosition(mParams.OriginTransform, hit.point);
                    }
                }
                
                // damage
            }
            else
            {
                trail.SetPosition(mParams.OriginTransform, spawnPos + (mParams.EyesTransform.forward * Data.Range));
                // mParams.Owner.Get()
                //     .DoCoroutine(SpawnTrail(trail, mParams.OriginTransform, spawnPos + (mParams.EyesTransform.forward * Data.Range), movement));
            }
            
            trail.SetDuration(.1f);
            trail.Enable(true);

            
        }

        // // When the trail collides with something
        // private IEnumerator SpawnTrail(TrailRenderer trail, Transform origin, Vector3 endPos, Vector3 endNormal, IMovement movement, float trailSpeed = 1000f)
        // {
        //     Debug.Log("Yes collision");
        //     var position = origin.position;
        //     var distance = Vector3.Distance(position + movement.Velocity * Time.deltaTime, endPos);
        //     var trailTime = distance / trailSpeed;
        //     var elapsed = 0f;
        //     
        //     trail.transform.position = position + movement.Velocity * Time.deltaTime;
        //     
        //     while (elapsed < trailTime)
        //     {
        //         var t = elapsed / trailTime;
        //         trail.transform.position = Vector3.Lerp(origin.position + movement.Velocity * Time.deltaTime, endPos, t);
        //         elapsed += Time.deltaTime;
        //
        //         yield return null;
        //     }
        //
        //     trail.transform.position = endPos;
        //     if (Data.Impact) Object.Instantiate(Data.Impact, endPos, Quaternion.LookRotation(endNormal));
        //     
        //     Object.Destroy(trail.gameObject, trail.time);
        // }
        //
        // private IEnumerator SpawnTrail(LineRenderer line, Transform origin, Vector3 endPos, IMovement movement, float duration = .1f)
        // {
        //     
        //     var elapsed = 0f;
        //
        //     var emitter = line.AddComponent<LineEmiter>();
        //     emitter.SetInfo(origin, endPos);
        //     
        //     while (elapsed < duration)
        //     {
        //         //yield return new WaitForEndOfFrame();
        //         //line.transform.position = Vector3.Lerp(origin.position + movement.Velocity * Time.deltaTime, endPos, t);
        //         elapsed += Time.deltaTime;
        //         
        //
        //         yield return null;
        //     }
        //     
        //     Object.Destroy(line.gameObject);
        // }
    }
}