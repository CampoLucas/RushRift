using System;
using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game
{
    public class VFXEmitter : MonoBehaviour, IPoolableObject<VFXEmitter, VFXEmitterParams>
    {
        public VFXEmitterParams Data { get; private set; }

        protected Transform Transform { get; private set; }
        protected IPoolObject<VFXEmitter, VFXEmitterParams> Pool { get; private set; }
        
        private void Awake()
        {
            Transform = transform;
        }
        
        public void PoolInit(IPoolObject<VFXEmitter, VFXEmitterParams> pool)
        {
            gameObject.SetActive(false);
            Pool = pool;
            
            OnPoolInit();
        }
        
        public void PoolDisable()
        {
            gameObject.SetActive(false);
            
            OnPoolDisable();
        }

        public void PoolReset(Vector3 pos, Quaternion rotation, VFXEmitterParams data)
        {
            Data = data;

            Transform.position = pos;
            Transform.rotation = rotation;
            Transform.localScale = Vector3.one * data.scale;
            gameObject.SetActive(true);
            
            OnPoolReset();
        }
        
        public void Dispose()
        {
            Pool.Remove(this);
            Pool = null;
            
            OnDispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        protected virtual void OnPoolInit() {}
        protected virtual void OnPoolDisable() {}
        protected virtual void OnPoolReset() {}
        protected virtual void OnDispose() {}
    }

    public struct VFXEmitterParams
    {
        public float scale;
        public Vector3 position;
        public Quaternion rotation;
    }
}