using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Game.LevelElements
{
    [DisallowMultipleComponent]
    public class OrbPickUp : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Collision effect component that detects player interaction.")]
        private EffectOnCollision effectOnCollision;

        [SerializeField, Tooltip("Looping VFX while the orb is available.")]
        private VisualEffect orbVFX;

        [SerializeField, Tooltip("One-shot VFX when the orb fades out on pickup.")]
        private ParticleSystem orbFadeVFX;

        [SerializeField, Tooltip("Light used to make the orb glow.")]
        private Light orbLight;

        [FormerlySerializedAs("destroyTime")]
        [Header("Settings")]
        [SerializeField, Tooltip("Seconds the orb light takes to fade after pickup.")]
        private float fadeTime = 1f;

        [SerializeField, Tooltip("If enabled, the orb will respawn after a delay.")]
        private bool isRespawnEnabled = true;

        [SerializeField, Tooltip("Seconds before the orb respawns (only if respawn is enabled).")]
        private float respawnTime = 10f;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        private bool isDebugLoggingEnabled = false;

        [SerializeField, Tooltip("Draw gizmos for orb state and light range.")]
        private bool drawGizmos = true;

        private bool _disabled;
        private float _lightStartIntensity;
        private float _timer;

        private void Awake()
        {
            if (!effectOnCollision) effectOnCollision = GetComponent<EffectOnCollision>();
            if (orbLight) _lightStartIntensity = orbLight.intensity;
            ClampConfig();
            Log("Awake");
        }

        private void Start()
        {
            if (effectOnCollision != null)
                effectOnCollision.OnApplied += OnPickUpHandler;
        }

        private void Update()
        {
            if (!_disabled) return;
            if (!isRespawnEnabled) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _disabled = false;
                EnableEffect();
                if (effectOnCollision != null)
                    effectOnCollision.OnApplied += OnPickUpHandler;
                Log("Respawned");
            }
        }

        private void OnPickUpHandler()
        {
            if (_disabled) return;

            ScreenFlash.Instance.TriggerFlash("#F1FA00", 0.1f, 0.1f);
            AudioManager.Play("OrbPickUp");

            if (orbVFX) orbVFX.gameObject.SetActive(false);
            if (orbFadeVFX) orbFadeVFX.Play();

            StartCoroutine(FadeLight());

            if (effectOnCollision != null)
                effectOnCollision.OnApplied -= OnPickUpHandler;
        }

        private void OnDestroy()
        {
            if (effectOnCollision != null && effectOnCollision.OnApplied != null)
                effectOnCollision.OnApplied -= OnPickUpHandler;

            StopAllCoroutines();
            effectOnCollision = null;
            orbVFX = null;
            orbFadeVFX = null;
            orbLight = null;
        }

        private IEnumerator FadeLight()
        {
            float t = fadeTime;
            float startRange = orbLight ? orbLight.range : 0f;
            float startIntensity = orbLight ? orbLight.intensity : 0f;

            while (t > 0f)
            {
                t -= Time.deltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeTime));

                if (orbLight)
                {
                    orbLight.intensity = Mathf.Lerp(0f, startIntensity, k);
                    orbLight.range = Mathf.Lerp(0.1f, startRange, k);
                }

                yield return null;
            }

            if (orbLight) orbLight.intensity = 0f;
            DisableEffect();
        }

        private void EnableEffect()
        {
            _disabled = false;

            if (orbLight)
            {
                orbLight.enabled = true;
                orbLight.intensity = _lightStartIntensity;
            }

            if (effectOnCollision)
            {
                effectOnCollision.enabled = true;
                effectOnCollision.ResetEffect();
            }

            if (orbVFX)
            {
                orbVFX.enabled = true;
                orbVFX.gameObject.SetActive(true);
                orbVFX.Play();
            }

            if (orbFadeVFX) orbFadeVFX.Stop();

            Log("Enabled");
        }

        private void DisableEffect()
        {
            _disabled = true;

            _timer = isRespawnEnabled ? respawnTime : 0f;

            if (orbLight) orbLight.enabled = false;
            if (effectOnCollision) effectOnCollision.enabled = false;
            if (orbVFX) orbVFX.enabled = false;
            if (orbFadeVFX) orbFadeVFX.Stop();

            Log(isRespawnEnabled ? $"Disabled. Respawning in {respawnTime:0.##}s" : "Disabled. No respawn");
        }

        private void ClampConfig()
        {
            fadeTime = Mathf.Max(0f, fadeTime);
            respawnTime = Mathf.Max(0f, respawnTime);
        }

        private void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[OrbPickUp] {name}: {msg}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampConfig();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Color c = _disabled ? new Color(1f, 0.5f, 0.2f, 0.85f) : new Color(0.2f, 1f, 0.6f, 0.85f);
            Gizmos.color = c;

            Vector3 p = transform.position;
            Gizmos.DrawWireSphere(p, orbLight ? Mathf.Max(0.05f, orbLight.range) : 0.5f);

            if (_disabled && isRespawnEnabled)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.85f);
                float r = 0.15f + 0.05f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
                Gizmos.DrawSphere(p + Vector3.up * 0.25f, r * 0.25f);
            }
        }
#endif
    }
}
