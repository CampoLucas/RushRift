using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class LensDistortionPlayer : MonoBehaviour
{
    [Header("Volume Binding")]
    [SerializeField, Tooltip("Volume that holds the post-processing profile with Lens Distortion.")]
    private Volume targetVolume;
    [SerializeField, Tooltip("Automatically bind the local Volume component if none is assigned.")]
    private bool autoBindLocalVolumeOnAwake = true;
    [SerializeField, Tooltip("Create and add a Lens Distortion override to the profile if none is found.")]
    private bool addOverrideIfMissing = true;

    [Header("Playback")]
    [SerializeField, Tooltip("Curve used to animate intensity over the duration. X = normalized time [0..1], Y = normalized value [0..1].")]
    private AnimationCurve intensityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField, Tooltip("Total animation time in seconds.")]
    private float durationSeconds = 0.6f;
    [SerializeField, Tooltip("Multiplier applied to the curve output after remap.")]
    private float intensityAmplitude = 1f;
    [SerializeField, Tooltip("Remap the curve's 0..1 output to this range before amplitude is applied.")]
    private Vector2 remapRange = new Vector2(0f, 0.5f);
    [SerializeField, Tooltip("Use unscaled time for the animation.")]
    private bool useUnscaledTime = true;
    [SerializeField, Tooltip("If true, adds the animated value to the initial intensity. If false, uses absolute values.")]
    private bool useRelativeIntensity = false;
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
    private Color gizmoPlayingColor = new Color(0.9f, 0.6f, 0.2f, 0.9f);
    [SerializeField, Tooltip("Gizmo color while idle.")]
    private Color gizmoIdleColor = new Color(0.2f, 1f, 0.6f, 0.9f);

    private static LensDistortionPlayer s_globalInstance;

    private LensDistortion lensDistortionOverride;
    private float initialIntensity;
    private Coroutine playCoroutine;
    private float lastEvaluatedIntensity;
    private bool isReady;

    public static LensDistortionPlayer GlobalInstance => s_globalInstance;

    private void Awake()
    {
        if (!targetVolume && autoBindLocalVolumeOnAwake)
            targetVolume = GetComponent<Volume>();

        isReady = TryBindLensDistortion(out lensDistortionOverride);
        if (isReady)
        {
            if (!lensDistortionOverride.intensity.overrideState) lensDistortionOverride.intensity.overrideState = true;
            initialIntensity = lensDistortionOverride.intensity.value;
            lastEvaluatedIntensity = initialIntensity;
        }

        if (registerAsGlobalInstance)
        {
            if (s_globalInstance && s_globalInstance != this && s_globalInstance.isActiveAndEnabled)
                Log("Replaced previous global instance");
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
        Play(intensityCurve, durationSeconds, intensityAmplitude, remapRange.x, remapRange.y, useUnscaledTime, useRelativeIntensity);
    }

    public void PlayStrong(float customAmplitude)
    {
        Play(intensityCurve, durationSeconds, Mathf.Max(0f, customAmplitude), remapRange.x, remapRange.y, useUnscaledTime, useRelativeIntensity);
    }

    public void PlayCustom(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled, bool relative)
    {
        Play(curve, duration, amplitude, remapMin, remapMax, unscaled, relative);
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

    private void Play(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled, bool relative)
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

        playCoroutine = StartCoroutine(PlayRoutine(curve, Mathf.Max(0f, duration), Mathf.Max(0f, amplitude), remapMin, remapMax, unscaled, relative));
    }

    private IEnumerator PlayRoutine(AnimationCurve curve, float duration, float amplitude, float remapMin, float remapMax, bool unscaled, bool relative)
    {
        float startTime = unscaled ? Time.unscaledTime : Time.time;
        float endTime = startTime + Mathf.Max(0.0001f, duration);

        while ((unscaled ? Time.unscaledTime : Time.time) < endTime)
        {
            float now = unscaled ? Time.unscaledTime : Time.time;
            float t = Mathf.InverseLerp(startTime, endTime, now);
            float c = Mathf.Clamp01(curve.Evaluate(t));
            float remapped = Mathf.Lerp(remapMin, remapMax, c);
            float value = Mathf.Clamp(remapped * amplitude, -1f, 1f);
            if (relative) value = Mathf.Clamp(initialIntensity + value, -1f, 1f);
            SetIntensityImmediate(value);
            yield return null;
        }

        float final = resetToInitialOnStop ? initialIntensity : Mathf.Clamp(remapMax * amplitude, -1f, 1f);
        if (useRelativeIntensity) final = resetToInitialOnStop ? initialIntensity : Mathf.Clamp(initialIntensity + remapMax * amplitude, -1f, 1f);
        SetIntensityImmediate(final);
        playCoroutine = null;
        Log("Play finished");
    }

    private bool TryBindLensDistortion(out LensDistortion ld)
    {
        ld = null;
        if (!targetVolume || !targetVolume.profile)
        {
            Log("Missing Volume or Profile");
            return false;
        }

        if (!targetVolume.profile.TryGet(out ld))
        {
            if (!addOverrideIfMissing)
            {
                Log("Lens Distortion override not found");
                return false;
            }

            ld = targetVolume.profile.Add<LensDistortion>(true);
            ld.active = true;
            ld.intensity.overrideState = true;
            ld.intensity.value = 0f;
            Log("Lens Distortion override added to profile");
        }

        return ld != null;
    }

    private void SetIntensityImmediate(float value)
    {
        if (!isReady) return;
        float v = Mathf.Clamp(value, -1f, 1f);
        lensDistortionOverride.intensity.value = v;
        lastEvaluatedIntensity = v;
    }

    private void ClampConfig()
    {
        durationSeconds = Mathf.Max(0f, durationSeconds);
        intensityAmplitude = Mathf.Max(0f, intensityAmplitude);
        if (remapRange.x > remapRange.y) remapRange = new Vector2(remapRange.y, remapRange.x);
        remapRange.x = Mathf.Clamp(remapRange.x, -1f, 1f);
        remapRange.y = Mathf.Clamp(remapRange.y, -1f, 1f);
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[LensDistortionPlayer] {name}: {msg}", this);
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

        float k = Mathf.InverseLerp(-1f, 1f, Mathf.Clamp(lastEvaluatedIntensity, -1f, 1f));
        Vector3 fill = Vector3.Lerp(a, b, k);
        Gizmos.DrawLine(a, fill);
        Gizmos.DrawCube(fill, new Vector3(0.01f, barH, 0.01f));
    }
#endif
}