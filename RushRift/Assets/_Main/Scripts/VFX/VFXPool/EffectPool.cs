using System;
using System.Collections.Generic;
using Game.DesignPatterns.Pool;
using Game.Entities;
using MyTools.Global;
using UnityEngine;

namespace Game.VFX
{
    [System.Serializable]
    public class EffectPool : IDisposable
    {
        [SerializeField] private VFXPrefabDictionarySO prefabDictionary;
        //[SerializeField] private SerializedDictionary<string, EffectEmitter> vfxPrefabs = new();

        private Dictionary<VFXPrefabID, IPoolObject<EffectEmitter, VFXEmitterParams>> _vfxDictionary = new();

        public bool TryGetVFX(VFXPrefabID id, VFXEmitterParams vfxEmitterParams, out EffectEmitter poolable)
        {
            // Check if there is a prefab with that id
            if (!prefabDictionary.TryGet(id, out var poolablePrefab))
            {
                poolable = null;
                return false;
            }

            // Check if there is a pool created with that id
            if (!_vfxDictionary.TryGetValue(id, out var pool))
            {
                pool = new PoolObject<EffectEmitter, VFXEmitterParams>(new EffectFactory(poolablePrefab), true);
                _vfxDictionary[id] = pool;
            }

            return pool.TryGet(vfxEmitterParams.position, vfxEmitterParams.rotation, vfxEmitterParams, out poolable);
        }

        public void PoolDisableAll()
        {
            var pools = _vfxDictionary.Values;
            
            if (pools.Count == 0) return;

            foreach (var pool in pools)
            {
                pool.RecycleAll();
            }
        }

        public void Dispose()
        {
            prefabDictionary = null;

            foreach (var pools in _vfxDictionary)
            {
                pools.Value.Dispose();
            }
            
            _vfxDictionary.Clear();
            _vfxDictionary = null;
        }
    }
}
