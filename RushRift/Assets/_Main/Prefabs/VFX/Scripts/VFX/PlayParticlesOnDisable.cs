using Game;
using Game.VFX;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("FX/Play Particles On Disable")]
public sealed class PlayParticlesOnDisable : MonoBehaviour
{
    [Header("VFX Prefab ID")]
    [SerializeField] private VFXPrefabID vfxID;

    [Header("Spawn Options")]
    [SerializeField] private bool matchRotation = true;
    [SerializeField] private bool matchScale = false;
    [SerializeField] private Transform overrideSpawnPoint;

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        var spawnXform = overrideSpawnPoint != null ? overrideSpawnPoint : transform;
        var rot = matchRotation ? spawnXform.rotation : Quaternion.identity;
        var pos = spawnXform.position;
        var scale = matchScale ? spawnXform.lossyScale.magnitude : 1f;

        var vfxParams = new VFXEmitterParams
        {
            position = pos,
            rotation = rot,
            scale = scale
        };

        EffectManager.TryGetVFX(vfxID, vfxParams, out _);
    }
}