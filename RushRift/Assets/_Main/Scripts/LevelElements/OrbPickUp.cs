using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Game.LevelElements
{
    public class OrbPickUp : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EffectOnCollision effectOnCollision;
        [SerializeField] private VisualEffect orbVFX;
        [SerializeField] private ParticleSystem orbFadeVFX;
        [SerializeField] private Light orbLight;

        [FormerlySerializedAs("destroyTime")]
        [Header("Settings")]
        [SerializeField] private float fadeTime = 1;
        [SerializeField] private float respawnTime = 10;

        private bool _disabled;
        private float _lightStartIntensity;
        private float _timer;
        

        private void Awake()
        {
            if (effectOnCollision == null) effectOnCollision = GetComponent<EffectOnCollision>();
            _lightStartIntensity = orbLight.intensity;
        }
        

        private void Start()
        {
            effectOnCollision.OnApplied += OnPickUpHandler;
        }

        private void Update()
        {
            if (!_disabled) return;

            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                _disabled = false;
                EnableEffect();
                effectOnCollision.OnApplied += OnPickUpHandler;
            }
        }

        private void OnPickUpHandler()
        {
            if (_disabled) return;
            
            ScreenFlash.Instance.TriggerFlash("#F1FA00", 0.1f, 0.1f);
            AudioManager.Play("OrbPickUp");
            
            orbVFX.gameObject.SetActive(false);
            orbFadeVFX.Play();
            StartCoroutine(FadeLight());
            
            effectOnCollision.OnApplied -= OnPickUpHandler;
            //Destroy(gameObject, destroyTime);
        }

        private void OnDestroy()
        {
            if (effectOnCollision.OnApplied != null) effectOnCollision.OnApplied -= OnPickUpHandler;
            StopAllCoroutines();

            effectOnCollision = null;
            orbVFX = null;
            orbFadeVFX = null;
            orbLight = null;
        }

        private IEnumerator FadeLight()
        {
            var t = fadeTime;
            var range = orbLight.range;
            var intensity = orbLight.intensity;
            
            while (t > 0)
            {
                t -= Time.deltaTime;

                orbLight.intensity = Mathf.Lerp(0, intensity, t / fadeTime);
                orbLight.range = Mathf.Lerp(0.1f, range, t / fadeTime);
                yield return null;
            }

            orbLight.intensity = 0;
            DisableEffect();
            //gameObject.SetActive(false);
        }

        private void EnableEffect()
        {
            // Set the disable flag as false
            _disabled = false;
            
            // Enable the components
            orbLight.enabled = true;
            orbLight.intensity = _lightStartIntensity;
            effectOnCollision.enabled = true;
            effectOnCollision.ResetEffect();
            orbVFX.enabled = true;
            orbVFX.gameObject.SetActive(true);
            
            // Start the orb bfx
            orbVFX.Play();
            orbFadeVFX.Stop();
        }

        private void DisableEffect()
        {
            // Set the disable flag as true
            _disabled = true;
            
            _timer = respawnTime;
            orbLight.enabled = false;
            effectOnCollision.enabled = false;
            orbVFX.enabled = false;
            orbFadeVFX.Stop();
        }
    }
}