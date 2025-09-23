using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public abstract class VolumeEffectPlayerBase<TOverride> : MonoBehaviour where TOverride : VolumeComponent
    {
        [Header("Volume Binding")]
        [SerializeField, Tooltip("Volume that holds the post-processing profile for this effect.")]
        protected Volume targetVolume;
        [SerializeField, Tooltip("Automatically bind the local Volume component if none is assigned.")]
        protected bool autoBindLocalVolumeOnAwake = true;
        [SerializeField, Tooltip("Create and add the effect override to the profile if none is found.")]
        protected bool addOverrideIfMissing = true;

        [Header("Playback")]
        [SerializeField, Tooltip("Curve used to animate intensity over the duration [0..1].")]
        protected AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("Total animation time in seconds.")]
        protected float durationSeconds = 0.4f;
        [SerializeField, Tooltip("Multiplier applied to the curve output after remap.")]
        protected float intensityAmplitude = 1f;
        [SerializeField, Tooltip("Remap the curve's 0..1 output to this range before amplitude is applied.")]
        protected Vector2 remapRange = new(0f, 1f);
        [SerializeField, Tooltip("Use unscaled time for the animation.")]
        protected bool useUnscaledTime = true;
        [SerializeField, Tooltip("If true, playing while already playing will restart from time 0. If false, new requests are ignored until finished.")]
        protected bool restartIfAlreadyPlaying = true;
        [SerializeField, Tooltip("Return to the initial intensity after the animation completes or is stopped.")]
        protected bool resetToInitialOnStop = true;

        [Header("Global Access")]
        [SerializeField, Tooltip("Register this player as a global instance so other scripts can trigger it statically.")]
        protected bool registerAsGlobalInstance = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        protected bool isDebugLoggingEnabled;
        [SerializeField, Tooltip("Draw gizmos for the current state.")]
        protected bool drawGizmos = true;
        [SerializeField, Tooltip("Gizmo color while playing.")]
        protected Color gizmoPlayingColor = new(0.2f, 0.9f, 1f, 0.9f);
        [SerializeField, Tooltip("Gizmo color while idle.")]
        protected Color gizmoIdleColor = new(0.2f, 1f, 0.6f, 0.9f);

        protected TOverride EffectOverride;
        protected float InitialIntensity;
        protected float LastEvaluatedIntensity;
        protected bool IsReady;

        private Coroutine playCoroutine;
        private Volume _cachedVolume;
        private bool _createdVolumeRuntime;

        protected abstract bool TryBindEffect(VolumeProfile profile, out TOverride effect);
        protected abstract UnityEngine.Rendering.FloatParameter GetIntensityParameter(TOverride effect);
        protected abstract float ClampValue(float v);
        protected virtual float MapFinalValue(bool isFinalPhase, float baseValue, float remapMaxTimesAmplitude) =>
            isFinalPhase && resetToInitialOnStop ? InitialIntensity : baseValue;

        protected virtual void RegisterGlobalInstance() { }
        protected virtual void UnregisterGlobalInstance() { }

        protected virtual void Awake()
        {
            if (!targetVolume && autoBindLocalVolumeOnAwake)
                targetVolume = GetComponent<Volume>();
            EnsureVolumeIfNeeded();

            IsReady = targetVolume && targetVolume.profile && TryBindEffect(targetVolume.profile, out EffectOverride);
            if (IsReady)
            {
                var p = GetIntensityParameter(EffectOverride);
                if (!p.overrideState) p.overrideState = true;
                InitialIntensity = p.value;
                LastEvaluatedIntensity = InitialIntensity;
            }

            ClampConfig();
            if (registerAsGlobalInstance) RegisterGlobalInstance();
            Log("Awake");
        }

        protected virtual void OnDisable()
        {
            if (playCoroutine != null) { StopCoroutine(playCoroutine); playCoroutine = null; }
            if (IsReady && resetToInitialOnStop) SetIntensityImmediate(InitialIntensity);

            if (_createdVolumeRuntime && _cachedVolume)
            {
                if (_cachedVolume.profile) Destroy(_cachedVolume.profile);
                Destroy(_cachedVolume.gameObject);
            }
            _cachedVolume = null;
            _createdVolumeRuntime = false;
        }

        protected virtual void OnDestroy()
        {
            UnregisterGlobalInstance();
            if (_createdVolumeRuntime && _cachedVolume)
            {
                if (_cachedVolume.profile) Destroy(_cachedVolume.profile);
                Destroy(_cachedVolume.gameObject);
            }
            _cachedVolume = null;
            _createdVolumeRuntime = false;
        }

        public void PlayOnce() => Play(intensityCurve, durationSeconds, intensityAmplitude, remapRange.x, remapRange.y, useUnscaledTime);
        public void PlayStrong(float customAmplitude) => Play(intensityCurve, durationSeconds, Mathf.Max(0f, customAmplitude), remapRange.x, remapRange.y, useUnscaledTime);
        public void PlayCustom(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled) =>
            Play(curve, duration, amplitude, remapMin, remapMax, unscaled);

        public void StopAndReset()
        {
            if (playCoroutine != null) { StopCoroutine(playCoroutine); playCoroutine = null; }
            if (IsReady && resetToInitialOnStop) SetIntensityImmediate(InitialIntensity);
            Log("Stopped");
        }

        private void Play(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled)
        {
            if (!IsReady) { Log("Play ignored: not ready"); return; }

            if (playCoroutine != null)
            {
                if (restartIfAlreadyPlaying) { StopCoroutine(playCoroutine); playCoroutine = null; }
                else { Log("Play ignored: already playing"); return; }
            }

            playCoroutine = StartCoroutine(PlayRoutine(curve, Mathf.Max(0f, duration), Mathf.Max(0f, amplitude), remapMin, remapMax, unscaled));
        }

        private IEnumerator PlayRoutine(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled)
        {
            float startTime = unscaled ? Time.unscaledTime : Time.time;
            float endTime = startTime + Mathf.Max(0.0001f, duration);

            while ((unscaled ? Time.unscaledTime : Time.time) < endTime)
            {
                float now = unscaled ? Time.unscaledTime : Time.time;
                float t = Mathf.InverseLerp(startTime, endTime, now);
                float c = Mathf.Clamp01(curve.Evaluate(t));
                float remapped = Mathf.Lerp(remapMin, remapMax, c);
                float v = ClampValue(remapped * amplitude);
                SetIntensityImmediate(v);
                yield return null;
            }

            float finalMapped = ClampValue(remapMax * amplitude);
            float final = MapFinalValue(true, finalMapped, finalMapped);
            SetIntensityImmediate(final);
            playCoroutine = null;
            Log("Play finished");
        }

        protected void SetIntensityImmediate(float value)
        {
            if (!IsReady) return;
            var p = GetIntensityParameter(EffectOverride);
            float v = ClampValue(value);
            p.value = v;
            LastEvaluatedIntensity = v;
        }

        protected void ClampConfig()
        {
            durationSeconds = Mathf.Max(0f, durationSeconds);
            intensityAmplitude = Mathf.Max(0f, intensityAmplitude);
            if (remapRange.x > remapRange.y) remapRange = new Vector2(remapRange.y, remapRange.x);
        }

        private void EnsureVolumeIfNeeded()
        {
            if (targetVolume) return;

            _cachedVolume = GetComponent<Volume>();
            if (_cachedVolume) { targetVolume = _cachedVolume; return; }

            var any = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            
            for (int i = 0; i < any.Length; i++)
            {
                if (any[i] && any[i].isGlobal)
                {
                    _cachedVolume = any[i];
                    _createdVolumeRuntime = false;
                    targetVolume = _cachedVolume;
                    return;
                }
            }

            var go = new GameObject("GlobalVolume (Runtime)");
            _cachedVolume = go.AddComponent<Volume>();
            _cachedVolume.isGlobal = true;
            _cachedVolume.priority = 100f;
            
            if (_cachedVolume.profile == null) _cachedVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _createdVolumeRuntime = true;
            targetVolume = _cachedVolume;
        }

        protected void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[{GetType().Name}] {name}: {msg}", this);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() { ClampConfig(); }

        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            Color c = (playCoroutine != null) ? gizmoPlayingColor : gizmoIdleColor;
            Gizmos.color = c;

            Vector3 p = transform.position;
            float r = 0.25f;
            Gizmos.DrawWireSphere(p, r);

            float barW = 0.3f;
            Vector3 a = p + Vector3.right * (-barW * 0.5f) + Vector3.up * 0.15f;
            Vector3 b = p + Vector3.right * (barW * 0.5f) + Vector3.up * 0.15f;
            Gizmos.DrawLine(a, b);

            float k = Mathf.InverseLerp(0f, 1f, LastEvaluatedIntensity);
            Vector3 fill = Vector3.Lerp(a, b, k);
            Gizmos.DrawLine(a, fill);
        }
#endif
    }
}