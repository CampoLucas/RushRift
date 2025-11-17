using Game.Utils;
using UnityEngine;

namespace Game.VFX
{
    [System.Serializable]
    public struct VFXEmitterEntry
    {
        [SerializeField] private VFXPrefabID id;

        [Header("Settings")]
        [SerializeField] private Transform origin;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float scale;
        [SerializeField] private bool matchScale;
        [SerializeField] private bool matchRotation;

        public VFXEmitter GetVFXEmitter(Transform defaultTr)
        {
            var tr = origin ? origin : defaultTr;
            if (!tr)
            {
                Debug.LogError("[ERROR] VFXEmitterEntry error trying to spawn a emitter with a null transform");
                return null;
            }

            var r = matchRotation ? tr.rotation : Quaternion.identity;
            var p = tr.GetOffsetPos(offset);
            var s = (matchScale ? tr.localScale.magnitude : 1) * scale;
            
            EffectManager.TryGetVFX(id, new VFXEmitterParams()
            {
                position = p,
                rotation = r,
                scale = s,
            }, out var emitter);

            return emitter;
        }
    }
}