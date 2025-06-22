using System;
using System.Collections.Generic;
using Game.DesignPatterns.Pool;
using Game.Entities;
using MyTools.Global;
using UnityEngine;

namespace Game
{
    [System.Serializable]
    public class VFXPool : IDisposable
    {
        [SerializeField] private SerializedDictionary<string, VFXEmitter> vfxPrefabs = new();

        private Dictionary<string, IPoolObject<VFXEmitter, VFXEmitterParams>> _vfxDictionary = new();

        public bool TryGetVFX(string id, VFXEmitterParams vfxEmitterParams, out VFXEmitter poolable)
        {
            // Check if there is a prefab with that id
            if (!vfxPrefabs.TryGetValue(id, out var poolablePrefab))
            {
                poolable = null;
                return false;
            }

            // Check if there is a pool created with that id
            if (!_vfxDictionary.TryGetValue(id, out var pool))
            {
                pool = new PoolObject<VFXEmitter, VFXEmitterParams>(new VFXFactory(poolablePrefab), true);
            }

            return pool.TryGet(vfxEmitterParams.position, vfxEmitterParams.rotation, vfxEmitterParams, out poolable);
        }

        public void Dispose()
        {
            vfxPrefabs = null;

            foreach (var pools in _vfxDictionary)
            {
                pools.Value.Dispose();
            }
            
            _vfxDictionary.Clear();
            _vfxDictionary = null;
        }
    }
}
