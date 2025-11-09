using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("FX/Play Particles On Disable")]
public sealed class PlayParticlesOnDisable : MonoBehaviour
{
    [Header("Particle Prefab")]
    [SerializeField] private ParticleSystem particlePrefab;

    [Header("Spawn Options")]
    [SerializeField] private bool matchRotation = true;
    [SerializeField] private bool matchScale = false; // usually false for particles
    [SerializeField] private Transform overrideSpawnPoint; // optional

    [Header("Cleanup")]
    [SerializeField] private bool autoDestroyInstance = true;

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (particlePrefab == null) return;

        Transform spawnXform = overrideSpawnPoint != null ? overrideSpawnPoint : transform;
        Quaternion rot = matchRotation ? spawnXform.rotation : Quaternion.identity;
        Vector3 pos = spawnXform.position;

        // Instantiate detached so it plays even if this GO is disabled
        ParticleSystem inst = Instantiate(particlePrefab, pos, rot);
        if (matchScale) inst.transform.localScale = spawnXform.lossyScale;

        // Optional: copy layer for correct rendering/visibility
        inst.gameObject.layer = gameObject.layer;

        // Play now
        inst.Play(true);

        // Auto-cleanup
        if (autoDestroyInstance)
        {
            var main = inst.main;
            float maxLifetime =
                main.duration +
                (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : (main.startLifetime.mode == ParticleSystemCurveMode.TwoCurves
                        ? main.startLifetime.constant
                        : main.startLifetime.constant));

            Destroy(inst.gameObject, Mathf.Max(0.01f, maxLifetime + 0.25f));
        }
    }
}