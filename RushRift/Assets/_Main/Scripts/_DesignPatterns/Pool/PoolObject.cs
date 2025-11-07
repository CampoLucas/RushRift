using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Factory;
using UnityEngine;

namespace Game.DesignPatterns.Pool
{
    public class Pool<TPoolable, TFactory> : IPool<TPoolable>
        where TPoolable : IDisposable where TFactory : IDisposable
    {
        protected List<TPoolable> InUse = new();
        protected List<TPoolable> Available = new();
        protected TFactory Factory;
        protected bool Disposed = false;
        private bool DisposeFactory;

        protected Pool(TFactory factory, bool disposeFactory)
        {
            Factory = factory;
            DisposeFactory = disposeFactory;
        }

        public virtual void Recycle(TPoolable poolable)
        {
            if (Disposed || !InUse.Remove(poolable)) return;
            
            Available.Add(poolable);
        }

        public virtual void RecycleAll()
        {
            for (var i = 0; i < InUse.Count; i++)
            {
                var element = InUse[i];
                
                if (element == null) continue;
                Recycle(element);
            }
        }

        public virtual void Remove(TPoolable poolable)
        {
            if (Disposed) return;
            Available.Remove(poolable);
            InUse.Remove(poolable);
        }
        
        public virtual void Dispose()
        {
            Disposed = true;
            for (var i = 0; i < InUse.Count; i++)
            {
                InUse[i].Dispose();
            }
            InUse.Clear();
            InUse = null;

            for (var i = 0; i < Available.Count; i++)
            {
                Available[i].Dispose();
            }
            Available.Clear();
            Available = null;
            
            if (DisposeFactory) Factory.Dispose();

        }
    }
    
    public class PoolObject<TPoolable, TData> : Pool<TPoolable, IFactory<TPoolable, TData>>, IPoolObject<TPoolable, TData> 
        where TPoolable : IPoolableObject<TPoolable, TData>
    {
        public PoolObject(IFactory<TPoolable, TData> factory, bool disposeFactory = false) : base(factory, disposeFactory)
        {
        }
        
        public bool TryGet(Vector3 position, Quaternion rotation, TData data, out TPoolable poolable)
        {
            if (Disposed)
            {
                poolable = default;
                return false;
            }

            if (Available.Count > 0)
            {
                poolable = Available[0];
                Available.RemoveAt(0);
            }
            else
            {
                poolable = Factory.Create();
                poolable.PoolInit(this);
            }
            
            poolable.PoolReset(position, rotation, data);
            InUse.Add(poolable);
            return true;
        }

        public TPoolable Get(Vector3 position, Quaternion rotation, TData data)
        {
            if (Disposed) return default;
            
            TPoolable poolable;
            
            if (Available.Count > 0)
            {
                poolable = Available[0];
                Available.RemoveAt(0);
            }
            else
            {
                poolable = Factory.Create();
                poolable.PoolInit(this);
            }
            
            poolable.PoolReset(position, rotation, data);
            InUse.Add(poolable);

            return poolable;
        }

        public override void Recycle(TPoolable poolable)
        {
            if (Disposed) return;
            base.Recycle(poolable);
            poolable.PoolDisable();
        }
    }
    
    public class PoolObject<T> : Pool<T, IFactory<T>>, IPoolObject<T> 
        where T : IPoolableObject<T>
    {
        public PoolObject(IFactory<T> factory, bool disposeFactory = false) : base(factory, disposeFactory) { }
        
        public bool TryGet(Vector3 position, Quaternion rotation, out T poolable)
        {
            if (Disposed)
            {
                poolable = default;
                return false;
            }

            if (Available.Count > 0)
            {
                poolable = Available[0];
                Available.RemoveAt(0);
            }
            else
            {
                poolable = Factory.Create();
                poolable.PoolInit(this);
            }
            
            poolable.PoolReset(position, rotation);
            InUse.Add(poolable);
            return true;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            if (Disposed) return default;
            T poolable;
            
            if (Available.Count > 0)
            {
                poolable = Available[0];
                Available.RemoveAt(0);
            }
            else
            {
                poolable = Factory.Create();
                poolable.PoolInit(this);
            }
            
            poolable.PoolReset(position, rotation);
            InUse.Add(poolable);

            return poolable;
        }

        public override void Recycle(T poolable)
        {
            base.Recycle(poolable);
            poolable.PoolDisable();
        }
    }
}
