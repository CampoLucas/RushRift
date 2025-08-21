using System.Collections.Generic;
using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashDamageStrategy : IDashUpdateStrategy
    {
        private IDetection _detection;
        private HashSet<GameObject> _damagedEntities = new();
        private DashDamageConfig _config;

        public DashDamageStrategy(DashDamageConfig config)
        {
            _config = config;
            
            
        }

        public void OnReset()
        {
            _damagedEntities.Clear();
        }
        
        public bool OnUpdate(in MotionContext context, in float delta)
        {
            return false;
        }

        public bool OnLateUpdate(in MotionContext context, in float delta)
        {
            return false;
            
//             Debug.Log("SuperTest: Damage Dash update");
//             if (_detection == null)
//             {
// #if UNITY_EDITOR
//                 Debug.Log("SuperTest: Detection created.");
// #endif
//                 _detection = _config.DetectionData.Get(context.Origin);
//             }
//
//             _detection.Detect();
//
//             if (_detection.Overlaps > 0)
//             {
//                 // Turn on effect
//                 var collisions = _detection.Collisions;
//                 for (var i = 0; i < _detection.Overlaps; i++)
//                 {
//                     var other = collisions[i];
//
//                     if (other == null)
//                     {
//                         continue;
//                     }
//
//                     if (_damagedEntities.Contains(other.gameObject) || 
//                         !other.gameObject.TryGetComponent<IController>(out var controller))
//                     {
//                         continue;
//                     }
//                     
//                     // if (!other.gameObject.TryGetComponent<IController>(out var controller))
//                     // {
//                     //     continue;
//                     // }
//                     //
//                     // if (!controller.GetModel().TryGetComponent<HealthComponent>(out var health))
//                     // {
//                     //     continue;
//                     // }
//
//                     var model = controller.GetModel();
//                     if (model.TryGetComponent<HealthComponent>(out var health))
//                     {
//                         _damagedEntities.Add(other.gameObject);
//                         if (_config.InstaKill) health.Intakill(context.Position);
//                         else health.Damage(_config.Damage, context.Position);
//                     }
//                     else if (model.TryGetComponent<DestroyableComponent>(out var destroyable))
//                     {
//                         destroyable.DestroyEntity();
//                     }
//
//                     
//                 }
//                 
//                 Debug.Log("SuperTest: Detected something");
//
//                 return true;
//             }
//             // Turn off effect
//
//             Debug.Log("SuperTest: Detected nothing");
//             return false;
        }

        public bool OnCollision(in MotionContext context, in Collider other)
        {
            // if it doesn't work, cast a sphere cast, because it will technically only work on only one entity at a time
            if (_damagedEntities.Contains(other.gameObject) || 
                !other.gameObject.TryGetComponent<IController>(out var controller))
            {
                return true;
            }
            
            var model = controller.GetModel();
            if (model.TryGetComponent<HealthComponent>(out var health))
            {
                _damagedEntities.Add(other.gameObject);
                if (_config.InstaKill) health.Intakill(context.Position);
                else health.Damage(_config.Damage, context.Position);

                return _config.StopOnKilling;
            }
            
            if (model.TryGetComponent<DestroyableComponent>(out var destroyable))
            {
                destroyable.DestroyEntity();

                return _config.StopOnDestroy;
            }

            return true;
        }

        public void Dispose()
        {
            _detection?.Dispose();
            _detection = null;

            _damagedEntities.Clear();
            _damagedEntities = null;

            _config = null;
        }
    }
}