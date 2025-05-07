using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Factory;
using Game.DesignPatterns.Pool;
using Game.Entities;
using UnityEngine;

public class VFXPool : MonoBehaviour, IPoolObject<ParticleEmitter, float>, IFactory<ParticleEmitter, float>
{
    public ParticleEmitter Product => particleEmitter;
    
    [SerializeField] private ParticleEmitter particleEmitter;
    
    private static VFXPool _instance;
    private IPoolObject<ParticleEmitter, float> _pool;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        _pool = new PoolObject<ParticleEmitter, float>(this);
    }

    public static bool TryRecycle(ParticleEmitter particleEmitter)
    {
        if (_instance)
        {
            _instance.Recycle(particleEmitter);
            return true;
        }

        return false;
    }

    public static bool TryRemove(ParticleEmitter particleEmitter)
    {
        if (_instance)
        {
            _instance.Remove(particleEmitter);
            return true;
        }

        return false;
    }

    public static bool TryGetParticle(Vector3 position, Quaternion rotation, float data, out ParticleEmitter poolable)
    {
        if (_instance)
        {
            return _instance.TryGet(position, rotation, data, out poolable);
        }

        poolable = null;
        return false;
    }

    public void Recycle(ParticleEmitter poolable)
    {
        _pool.Recycle(poolable);
    }

    public void Remove(ParticleEmitter poolable)
    {
        _pool.Remove(poolable);
    }

    public bool TryGet(Vector3 position, Quaternion rotation, float data, out ParticleEmitter poolable)
    {
        return _pool.TryGet(position, rotation, data, out poolable);
    }

    public ParticleEmitter Get(Vector3 position, Quaternion rotation, float data)
    {
        return _pool.Get(position, rotation, data);
    }

    public ParticleEmitter Create()
    {
        var p = Instantiate(Product);

        return p;
    }

    public ParticleEmitter[] Create(int quantity)
    {
        var projectiles = new ParticleEmitter[quantity];
            
        for (var i = 0; i < quantity; i++)
        {
            projectiles[i] = Instantiate(Product);
        }

        return projectiles;
    }
    
    public void Dispose()
    {
        _pool.Dispose();
    }
}
