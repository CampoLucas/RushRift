using System;
using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game.Entities
{
    public class ParticleEmitter : MonoBehaviour, IPoolableObject<ParticleEmitter, float>
    {
        public float Data { get; private set; }
        
        [SerializeField] private ParticleSystem particle;
        
        private IPoolObject<ParticleEmitter, float> _pool;
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void Update()
        {
            if (particle.time >= particle.main.duration)
            {
                _pool.Recycle(this);
            }
        }

        public void PoolInit(IPoolObject<ParticleEmitter, float> poolObject)
        {
            gameObject.SetActive(false);
            _pool = poolObject;
        }
        
        public void PoolDisable()
        {
            particle.Stop();
            gameObject.SetActive(false);
        }

        public void PoolReset(Vector3 pos, Quaternion rotation, float data)
        {
            Data = data;

            _transform.position = pos;
            _transform.rotation = rotation;
            
            _transform.localScale = Vector3.one * data;
            gameObject.SetActive(true);
            particle.Play();
        }

        public void Dispose()
        {
            _pool = null;
            particle = null;
        }

    }
}

