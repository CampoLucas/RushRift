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
                // Turn on effect
                var collisions = _detection.Collisions;
                for (var i = 0; i < _detection.Overlaps; i++)
                {
                    var other = collisions[i];

                    if (other == null)
                    {
                        continue;
                    }

                    if (_damagedEntities.Contains(other.gameObject))
                    {
                        continue;
                    }
                    
                    if (!other.gameObject.TryGetComponent<IController>(out var controller))
                    {
                        continue;
                    }
                    
                    if (!controller.GetModel().TryGetComponent<HealthComponent>(out var health))
                    {
                        continue;
                    }

                    _damagedEntities.Add(other.gameObject);
                    health.Damage(_damage, currentPosition);
                }

                return true;
            }
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