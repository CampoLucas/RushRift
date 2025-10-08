using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.ScreenEffects
{
    /// <summary>
    /// UI-based screen fader that adjusts the alpha of a target Image.
    /// Uses an AnimationCurve to ease the transition.
    /// </summary>
    public class FadeScreenUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image fadeImage;

        [Header("Timing")]
        [Tooltip("Alpha change speed. Effective duration ≈ 1/speed.")]
        [SerializeField, Min(0.0001f)] private float speed = 1f;

        [Tooltip("Use unscaled time (ignores Time.timeScale).")]
        [SerializeField] private bool useUnscaledTime = true;

        [Tooltip("Start fully opaque, then fade to transparent on Awake.")]
        [SerializeField] private bool fadeInOnAwake = true;

        [Header("Smoothing")]
        [Tooltip("Evaluated from 0→1 over the fade. 0 = start alpha, 1 = target alpha.")]
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine _current;

        private void Reset()
        {
            if (!fadeImage) fadeImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (!fadeImage)
            {
                Debug.LogWarning("[FadeScreenUI] No Image assigned.");
                return;
            }

            var c = fadeImage.color;

            if (fadeInOnAwake)
            {
                c.a = 1f;
                fadeImage.color = c;
                FadeIn(); // go to alpha = 0 (transparent)
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(0.0001f, newSpeed);
        }

        public void FadeIn()  => PlayTo(0f);
        public void FadeOut() => PlayTo(1f);

        public IEnumerator FadeInRoutine()  => PlayToRoutine(0f);
        public IEnumerator FadeOutRoutine() => PlayToRoutine(1f);

        private void PlayTo(float targetAlpha)
        {
            if (!fadeImage) return;
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(FadeTo(targetAlpha));
        }

        private IEnumerator PlayToRoutine(float targetAlpha)
        {
            if (!fadeImage) yield break;
            if (_current != null) StopCoroutine(_current);
            yield return _current = StartCoroutine(FadeTo(targetAlpha));
            _current = null;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            var img = fadeImage;
            Color c = img.color;
            float startAlpha = c.a;

            if (Mathf.Approximately(startAlpha, targetAlpha))
            {
                c.a = targetAlpha;
                img.color = c;
                yield break;
            }

            float t = 0f;
            while (true)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt * speed;

                float t01   = Mathf.Clamp01(t);
                float eased = fadeCurve != null ? Mathf.Clamp01(fadeCurve.Evaluate(t01)) : t01;

                c.a = Mathf.Lerp(startAlpha, targetAlpha, eased);
                img.color = c;

                if (t >= 1f) break;
                yield return null;
            }
        }
    }
}