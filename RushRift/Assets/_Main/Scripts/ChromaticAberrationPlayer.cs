using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class ChromaticAberrationPlayer : MonoBehaviour
{
    [Header("Volume Binding")]
    [SerializeField, Tooltip("Volume that holds the post-processing profile with Chromatic Aberration.")]
    private Volume targetVolume;
    [SerializeField, Tooltip("Automatically bind the local Volume component if none is assigned.")]
    private bool autoBindLocalVolumeOnAwake = true;
    [SerializeField, Tooltip("Create and add a Chromatic Aberration override to the profile if none is found.")]
    private bool addOverrideIfMissing = true;

    [Header("Playback")]
    [SerializeField, Tooltip("Curve used to animate intensity over the duration. X = normalized time [0..1], Y = normalized value [0..1].")]
    private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField, Tooltip("Total animation time in seconds.")]
    private float durationSeconds = 0.4f;
    [SerializeField, Tooltip("Multiplier applied to the curve output.")]
    private float intensityAmplitude = 1f;
    [SerializeField, Tooltip("Remap the curve's 0..1 output to this range before amplitude is applied.")]
    private Vector2 remapRange = new Vector2(0f, 1f);
    [SerializeField, Tooltip("Use unscaled time for the animation.")]
    private bool useUnscaledTime = true;
    [SerializeField, Tooltip("If true, playing while already playing will restart from time 0. If false, new requests are ignored until finished.")]
    private bool restartIfAlreadyPlaying = true;
    [SerializeField, Tooltip("Return to the initial intensity after the animation completes or is stopped.")]
    private bool resetToInitialOnStop = true;

    [Header("Global Access")]
    [SerializeField, Tooltip("Register this player as a global instance so other scripts can trigger it statically.")]
    private bool registerAsGlobalInstance = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw gizmos for the current state.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("Gizmo color while playing.")]
    private Color gizmoPlayingColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    [SerializeField, Tooltip("Gizmo color while idle.")]
    private Color gizmoIdleColor = new Color(0.2f, 1f, 0.6f, 0.9f);

    private static ChromaticAberrationPlayer s_globalInstance;

    private ChromaticAberration chromaticAberrationOverride;
    private float initialIntensity;
    private Coroutine playCoroutine;
    private float lastEvaluatedIntensity;
    private bool isReady;

    public static ChromaticAberrationPlayer GlobalInstance => s_globalInstance;

    private void Awake()
    {
        if (!targetVolume && autoBindLocalVolumeOnAwake)
            targetVolume = GetComponent<Volume>();

        isReady = TryBindChromaticAberration(out chromaticAberrationOverride);
        if (isReady)
        {
            if (!chromaticAberrationOverride.intensity.overrideState) chromaticAberrationOverride.intensity.overrideState = true;
            initialIntensity = chromaticAberrationOverride.intensity.value;
            lastEvaluatedIntensity = initialIntensity;
        }

        if (registerAsGlobalInstance)
        {
            if (s_globalInstance && s_globalInstance != this && s_globalInstance.isActiveAndEnabled)
            {
                if (isDebugLoggingEnabled) Debug.Log("[ChromaticAberrationPlayer] Replaced previous global instance.", this);
            }
            s_globalInstance = this;
        }

        ClampConfig();
        Log("Awake");
    }

    private void OnDisable()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        if (isReady && resetToInitialOnStop)
            SetIntensityImmediate(initialIntensity);
    }

    public void PlayOnce()
    {
        Play(intensityCurve, durationSeconds, intensityAmplitude, remapRange.x, remapRange.y, useUnscaledTime);
    }

    public void PlayStrong(float customAmplitude)
    {
        Play(intensityCurve, durationSeconds, Mathf.Max(0f, customAmplitude), remapRange.x, remapRange.y, useUnscaledTime);
    }

    public void PlayCustom(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled)
    {
        Play(curve, duration, amplitude, remapMin, remapMax, unscaled);
    }

    public void StopAndReset()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        if (isReady && resetToInitialOnStop)
            SetIntensityImmediate(initialIntensity);

        Log("Stopped");
    }

    public static void PlayGlobal()
    {
        if (s_globalInstance) s_globalInstance.PlayOnce();
    }

    public static void PlayGlobalStrong(float amplitude)
    {
        if (s_globalInstance) s_globalInstance.PlayStrong(amplitude);
    }

    private void Play(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled)
    {
        if (!isReady)
        {
            Log("Play ignored: not ready");
            return;
        }

        if (playCoroutine != null)
        {
            if (restartIfAlreadyPlaying)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
            else
            {
                Log("Play ignored: already playing");
                return;
            }
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
            float value = Mathf.Clamp01(remapped * amplitude);
            SetIntensityImmediate(value);
            yield return null;
        }

        SetIntensityImmediate(resetToInitialOnStop ? initialIntensity : Mathf.Clamp01(remapMax * amplitude));
        playCoroutine = null;
        Log("Play finished");
    }

    private bool TryBindChromaticAberration(out ChromaticAberration ca)
    {
        ca = null;
        if (!targetVolume || !targetVolume.profile)
        {
            Log("Missing Volume or Profile");
            return false;
        }

        if (!targetVolume.profile.TryGet(out ca))
        {
            if (!addOverrideIfMissing)
            {
                Log("Chromatic Aberration override not found");
                return false;
            }

            ca = targetVolume.profile.Add<ChromaticAberration>(true);
            ca.active = true;
            ca.intensity.overrideState = true;
            ca.intensity.value = 0f;
            Log("Chromatic Aberration override added to profile");
        }

        return ca != null;
    }

    private void SetIntensityImmediate(float value)
    {
        if (!isReady) return;
        float v = Mathf.Clamp01(value);
        chromaticAberrationOverride.intensity.value = v;
        lastEvaluatedIntensity = v;
    }

    private void ClampConfig()
    {
        durationSeconds = Mathf.Max(0f, durationSeconds);
        intensityAmplitude = Mathf.Max(0f, intensityAmplitude);
        if (remapRange.x > remapRange.y) remapRange = new Vector2(remapRange.y, remapRange.x);
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[ChromaticAberrationPlayer] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ClampConfig();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Color c = playCoroutine != null ? gizmoPlayingColor : gizmoIdleColor;
        Gizmos.color = c;

        Vector3 p = transform.position;
        float r = 0.25f;
        Gizmos.DrawWireSphere(p, r);
        Gizmos.DrawSphere(p + Vector3.up * 0.05f, 0.02f);

        float barW = 0.3f;
        float barH = 0.04f;
        Vector3 a = p + Vector3.right * (-barW * 0.5f) + Vector3.up * 0.15f;
        Vector3 b = p + Vector3.right * (barW * 0.5f) + Vector3.up * 0.15f;
        Gizmos.DrawLine(a, b);

        float k = Mathf.Clamp01(lastEvaluatedIntensity);
        Vector3 fill = Vector3.Lerp(a, b, k);
        Gizmos.DrawLine(a, fill);
    }
#endif
}