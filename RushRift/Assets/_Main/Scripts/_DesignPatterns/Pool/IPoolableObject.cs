using System;
using Game.DesignPatterns.Factory;
using UnityEngine;

namespace Game.DesignPatterns.Pool
{
    public interface IPoolable<in TPool> : IDisposable
    {
        void PoolDisable();
        void PoolInit(TPool pool);
    }
    
    public interface IPoolableObject<TPoolable, TData> : IProduct<TData>, IPoolable<IPoolObject<TPoolable, TData>>
        where TPoolable : IPoolableObject<TPoolable, TData>
    {
        void PoolReset(Vector3 pos, Quaternion rotation, TData data);
    }

    public interface IPoolableObject<TPoolable> : IPoolable<IPoolObject<TPoolable>>
        where TPoolable : IPoolableObject<TPoolable>
    {
        void PoolReset(Vector3 pos, Quaternion rotation);
    }
}