using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Pool;
using Game.Entities.Components;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities
{
    public class Projectile : MonoBehaviour, IPoolableObject<Projectile, ProjectileData>
    {
        public ProjectileData Data => data;

        [FormerlySerializedAs("rb")]
        [Header("References")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private Collider collider;
        
        [Header("Data")]
        [SerializeField] private ProjectileData data;

        [Header("VFX")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private GameObject explosion;

        private Transform _transform;
        private float _timer;
        private IPoolObject<Projectile, ProjectileData> _poolObject;
        private GameObject _thrower;
        
        private int _wallCollisions;
        private int _enemyCollisions;
        private int _penetrations;
        private HashSet<GameObject> _collided = new();

        private void Awake()
        {
            _transform = transform;
            _timer = 0;
            if (!body) body = GetComponent<Rigidbody>();
            if (!collider) collider = GetComponent<Collider>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= data.LifeTime)
            {
                _poolObject?.Recycle(this);
                return;
            }

            if (body && trail)
            {
                trail.time = Mathf.Abs(1f / body.velocity.magnitude) * 3;
            }

            //_transform.position += _transform.forward * (data.Speed * Time.deltaTime);
        }

        public void SetThrower(GameObject thrower)
        {
            _thrower = thrower;
        }

        public void PoolInit(IPoolObject<Projectile, ProjectileData> poolObject)
        {
            gameObject.SetActive(false);
            _poolObject = poolObject;
        }

        public void SetData(ProjectileData newData)
        {
            data = newData;
            _transform.localScale = Vector3.one * data.Size;
            trail.widthMultiplier = data.Size;
            trail.time = data.Speed * .25f;

            
            Debug.Log($"Set gravity: {data.Gravity}");
            body.useGravity = data.Gravity;
        }
        
        public void PoolDisable()
        {
            trail.Clear();
            trail.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void ResetPoolable(Vector3 position, Quaternion rotation)
        {
            _wallCollisions = 0;
            _enemyCollisions = 0;
            _penetrations = 0;
            
            _transform.position = position;
            _transform.rotation = rotation;
            
            trail.gameObject.SetActive(true);
            gameObject.SetActive(true);
            trail.Clear();
            _timer = 0;
            
            _collided.Clear();
            
            body.velocity = Vector3.zero;
            body.AddForce(transform.forward * data.Speed, ForceMode.Impulse);
        }

        public void PoolReset(Vector3 position, Quaternion rotation, ProjectileData d)
        {
            SetData(d);
            
            ResetPoolable(position, rotation);
        }

        public void OnCollisionEnter(Collision other)
        {
            OnCollision(other.gameObject, other);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("TriggerEnter");
            OnTrigger(other.gameObject);
        }

        private void OnCollision(GameObject other, Collision otherColl)
        {
#if false
            if (other.CompareTag("Projectile")) return;
            if (other == _thrower) return;

            _collisions++;

            if (other.TryGetComponent<IEntityController>(out var controller) &&
                controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                healthComponent.Damage(Data.Damage, transform.position);
            }
            
            if (_collisions < data.WallBounce)
            {
                _timer-= _timer/_collisions;
                //_thrower = null; // That way it bounces and hits the entity that fires it
                //rb.AddForce(rb.velocity * 2);
                return;
            }
            
            //var e = Instantiate(explosion, transform.position, Quaternion.identity);
            //e.transform.localScale = Vector3.one * data.Size;
            VFXPool.TryGetParticle(transform.position, transform.rotation, Data.Size, out var p);
            if (_poolObject != null)
            {
                _poolObject.Recycle(this);
            }
            else
            {
                Destroy(gameObject);
            }    
#else
            // Check if the projectile collided with another projectile and return.
            if (other.CompareTag("Projectile")) return;
            
            // Check if it collided with the thrower and return.
            if (other == _thrower) return;

            var normal = otherColl.GetContact(0).normal;
            
            // Bounce or destroy
            if (other.layer == LayerMask.NameToLayer("Ground") || other.layer == LayerMask.NameToLayer("Wall") || other.layer == LayerMask.NameToLayer("Obstacle"))
            {
                if (_wallCollisions < data.WallBounce)
                {
                    _wallCollisions++;
                    //Debug.Log("Bounce");
                    //transform.rotation = Quaternion.LookRotation(BounceDirection(otherColl, _transform.forward));
                    
                }
                else
                {
                    ExplodeCollision(normal);
                }
            }
            else
            {
                ExplodeCollision(normal);
            }
            
            
#endif
        }

        private void OnTrigger(GameObject other)
        {
            if (other == _thrower) return;
            if (!_collided.Add(other)) return;

            if (other.TryGetComponent<IController>(out var controller) &&
                controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                healthComponent.Damage(Data.Damage, transform.position);
            }
            
            if (_enemyCollisions < data.EnemyBounce)
            {
                _enemyCollisions++;

                if (TryGetClosest(LayerMask.NameToLayer("Enemy"), _collided, out var closest))
                {
                    Debug.Log($"Closest {closest.name}");
                    var dir = (closest.transform.position - transform.position).normalized;

                    var velocityMag = body.velocity.magnitude;
                    body.velocity = dir * velocityMag;
                    //body.AddForce(dir * data.Speed, ForceMode.Impulse);
                    ExplodeCollision(Vector3.up, false);
                    return;
                    
                }
                
                
            }
            
            if (_penetrations < data.Penetration)
            {
                _penetrations++;
                ExplodeCollision(Vector3.up, false);

            }
            else
            {
                ExplodeCollision(Vector3.up);

            }
        }

        private bool TryGetClosest(int layer, HashSet<GameObject> ignore, out GameObject closest)
        {
            closest = null;

            // Get all active GameObjects in the scene
            var allObjects = GameObject.FindObjectsOfType<EntityController>();

            var closestDistance = float.MaxValue;
            var currentPosition = transform.position;

            foreach (var obj in allObjects)
            {
                // Skip if object is inactive, not in the target layer, or in the ignore list
                if (!obj.gameObject.activeInHierarchy || obj.gameObject.layer != layer || ignore.Contains(obj.gameObject))
                    continue;

                var distance = Vector3.Distance(currentPosition, obj.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = obj.gameObject;
                }
            }

            return closest != null;
        }

        private void ExplodeCollision(Vector3 normal, bool recycle = true)
        {
            //var e = Instantiate(explosion, transform.position, Quaternion.identity);
            //e.transform.localScale = Vector3.one * data.Size;
            VFXPool.TryGetParticle(transform.position, transform.rotation, Data.Size, out var p);
            p.transform.rotation = Quaternion.LookRotation(normal);
            
            if (!recycle) return;
            if (_poolObject != null)
            {
                _poolObject.Recycle(this);
            }
            else
            {
                Destroy(gameObject);
            }    
        }
        
        public void Dispose()
        {
            _poolObject.Remove(this);
            _poolObject = null;
        }
    }
}