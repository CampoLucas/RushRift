using System;
using Game.DesignPatterns.Factory;
using UnityEngine;

namespace Game.DesignPatterns.Pool
{
    public interface IPool<TPoolable> : IDisposable
    {
        void Recycle(TPoolable poolable);
        void Remove(TPoolable poolable);
    }
    
    public interface IPoolObject<TPoolable, in TData> : IPool<TPoolable>
        where TPoolable : IPoolableObject<TPoolable, TData>
    {
        bool TryGet(Vector3 position, Quaternion rotation, TData data, out TPoolable poolable);
        TPoolable Get(Vector3 position, Quaternion rotation, TData data);
    }

    public interface IPoolObject<TPoolable> : IPool<TPoolable>
        where TPoolable : IPoolableObject<TPoolable>
    {
        bool TryGet(Vector3 position, Quaternion rotation, out TPoolable poolable);
        TPoolable Get(Vector3 position, Quaternion rotation);
    }
}