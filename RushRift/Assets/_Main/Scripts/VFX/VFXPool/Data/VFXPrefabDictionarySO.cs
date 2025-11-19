using System.Collections;
using System.Collections.Generic;
using MyTools.Global;
using UnityEngine;

namespace Game.VFX
{
    [CreateAssetMenu(menuName = "Game/VFX/Prefab Dictionary")]
    public class VFXPrefabDictionarySO : ScriptableObject
    {
        [SerializeField] private SerializedDictionary<VFXPrefabID, VFXEmitter> prefabDictionary;

        public bool TryGet(in VFXPrefabID id, out VFXEmitter emitter)
        {
            return prefabDictionary.TryGetValue(id, out emitter);
        }

        // public bool TryGet<TEmitter>(in VFXPrefabID id, out TEmitter castedEmitter) where TEmitter : EffectEmitter
        // {
        //     if (prefabDictionary.TryGetValue(id, out var emitter))
        //     {
        //         castedEmitter = emitter as TEmitter;
        //         return castedEmitter != null;
        //     }
        //
        //     castedEmitter = null;
        //     return false;
        // }
    }

    
}
