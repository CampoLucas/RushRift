using System;
using System.Collections;
using UnityEngine;
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

        [Header("Settings")]
        [SerializeField] private float destroyTime = 1;

        private void Awake()
        {
            if (effectOnCollision == null) effectOnCollision = GetComponent<EffectOnCollision>();
        }

        private void Start()
        {
            effectOnCollision.OnApplied += OnPickUpHandler;
        }

        private void OnPickUpHandler()
        {
            ScreenFlash.Instance.TriggerFlash("#F1FA00");
            AudioManager.Play("OrbPickUp");
            
            orbVFX.gameObject.SetActive(false);
            orbFadeVFX.Play();
            StartCoroutine(FadeLight());
            Destroy(gameObject, destroyTime);
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
            var t = destroyTime;
            var range = orbLight.range;
            var intensity = orbLight.intensity;
            
            while (t > 0)
            {
                t -= Time.deltaTime;

                orbLight.intensity = Mathf.Lerp(0, intensity, t / destroyTime);
                orbLight.range = Mathf.Lerp(0.1f, range, t / destroyTime);
                yield return null;
            }

            orbLight.intensity = 0;
        }
    }
}