using System.Collections.Generic;
using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    public class DashDamage : IDashUpdateStrategy
    {
        private IDetection _detection;
        private HashSet<GameObject> _damagedEntities = new();
        private readonly float _damage;

        public DashDamage(Transform origin, IDetectionData detectData, float damage)
        {
            _detection = detectData.Get(origin);
            _damage = damage;
        }
        
        public bool OnDashUpdate(Transform transform, Vector3 currentPosition)
        {
            _detection.Detect();

            if (_detection.Overlaps > 0)
            {
                Debug.Log("SuperTest: does detect");
                // Turn on effect
                var collisions = _detection.Collisions;
                for (var i = 0; i < _detection.Overlaps; i++)
                {
                    var other = collisions[i];

                    if (other == null)
                    {
                        Debug.Log("SuperTest: other is null");
                        continue;
                    }

                    if (_damagedEntities.Contains(other.gameObject))
                    {
                        Debug.Log("SuperTest: already contains other");
                        continue;
                    }



                    if (!other.gameObject.TryGetComponent<IController>(out var controller))
                    {
                        Debug.Log("SuperTest: controller is null");
                        continue;
                    }
                    
                    if (!controller.GetModel().TryGetComponent<HealthComponent>(out var health))
                    {
                        Debug.Log("SuperTest: health component is null");
                        continue;
                    }

                    Debug.Log($"SuperTest: Damage {other.gameObject.name}");
                    _damagedEntities.Add(other.gameObject);
                    health.Damage(_damage, currentPosition);
                }

                return true;
            }
            Debug.Log("SuperTest: doesn't detect");
            // Turn off effect

            return false;
        }

        public void Reset()
        {
            _damagedEntities.Clear();
        }
        
        public void Dispose()
        {
            _detection.Dispose();
            _detection = null;
            
            _damagedEntities.Clear();
            _damagedEntities = null;
        }
    }
}