using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using _Main.Scripts.Feedbacks;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class VignettePlayer : VolumeEffectPlayerBase<Vignette>
{
    [Header("Vignette Color")]
    [SerializeField, Tooltip("Animate the vignette color while playing.")]
    private bool animateColor = true;
    [SerializeField, Tooltip("Curve used to animate the color blend over the duration [0..1].")]
    private AnimationCurve colorCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.06f, 1f), new Keyframe(0.94f, 1f), new Keyframe(1, 0));
    [SerializeField, Tooltip("Remap the color curve's 0..1 output to this range.")]
    private Vector2 colorRemapRange = new Vector2(0f, 1f);
    [SerializeField, Tooltip("Target color to blend towards while playing.")]
    private Color targetVignetteColor = Color.black;

    [Header("Intensity Options")]
    [SerializeField, Tooltip("If true, the animated intensity is added on top of the initial intensity.")]
    private bool relativeIntensity = false;

    [Header("Global Access")]
    [SerializeField, Tooltip("If true, registers this as a global instance to call via VignettePlayer.PlayGlobal().")]
    private bool registerAsGlobalVignette = true;

    private static VignettePlayer s_global;

    private Coroutine _vignetteRoutine;
    private Color _initialColor;

    protected override void Awake()
    {
        base.Awake();
        if (IsReady)
        {
            if (!EffectOverride.color.overrideState) EffectOverride.color.overrideState = true;
            _initialColor = EffectOverride.color.value;
        }
        if (registerAsGlobalVignette) RegisterGlobalInstance();
    }

    protected override void OnDisable()
    {
        if (_vignetteRoutine != null) { StopCoroutine(_vignetteRoutine); _vignetteRoutine = null; }
        if (IsReady && resetToInitialOnStop) EffectOverride.color.value = _initialColor;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        if (_vignetteRoutine != null) { StopCoroutine(_vignetteRoutine); _vignetteRoutine = null; }
        if (IsReady && resetToInitialOnStop) EffectOverride.color.value = _initialColor;
        UnregisterGlobalInstance();
        base.OnDestroy();
    }

    protected override bool TryBindEffect(VolumeProfile profile, out Vignette effect)
    {
        if (!profile.TryGet(out effect) && addOverrideIfMissing)
        {
            effect = profile.Add<Vignette>(true);
            effect.active = true;
            effect.intensity.overrideState = true;
            effect.intensity.value = 0f;
            effect.color.overrideState = true;
            effect.color.value = Color.black;
            Log("Vignette override added to profile");
        }
        return effect != null;
    }

    protected override FloatParameter GetIntensityParameter(Vignette effect) => effect.intensity;
    protected override float ClampValue(float v) => Mathf.Clamp01(v);

    protected override void RegisterGlobalInstance()
    {
        s_global = this;
    }

    protected override void UnregisterGlobalInstance()
    {
        if (s_global == this) s_global = null;
    }

    public void VignettePlay() => VignettePlay(targetVignetteColor, intensityCurve, durationSeconds, intensityAmplitude, remapRange.x, remapRange.y, colorCurve, colorRemapRange.x, colorRemapRange.y, useUnscaledTime);
    public void VignettePlay(Color color) => VignettePlay(color, intensityCurve, durationSeconds, intensityAmplitude, remapRange.x, remapRange.y, colorCurve, colorRemapRange.x, colorRemapRange.y, useUnscaledTime);
    public void VignettePlayStrong(float amplitude) => VignettePlay(targetVignetteColor, intensityCurve, durationSeconds, Mathf.Max(0f, amplitude), remapRange.x, remapRange.y, colorCurve, colorRemapRange.x, colorRemapRange.y, useUnscaledTime);

    public static void PlayGlobal() { if (s_global) s_global.VignettePlay(); }
    public static void PlayGlobal(Color color) { if (s_global) s_global.VignettePlay(color); }
    public static void PlayGlobalStrong(float amplitude) { if (s_global) s_global.VignettePlayStrong(amplitude); }

    public void VignettePlay(
        Color targetColor,
        AnimationCurve intensityAnim, float duration, float amplitude, float remapMin, float remapMax,
        AnimationCurve colorAnim, float colorRemapMin, float colorRemapMax,
        bool unscaledTime)
    {
        if (!IsReady) { Log("Play ignored: not ready"); return; }

        if (_vignetteRoutine != null)
        {
            if (restartIfAlreadyPlaying) { StopCoroutine(_vignetteRoutine); _vignetteRoutine = null; }
            else { Log("Play ignored: already playing"); return; }
        }

        _vignetteRoutine = StartCoroutine(PlayVignetteRoutine(targetColor, intensityAnim, Mathf.Max(0f, duration), Mathf.Max(0f, amplitude), remapMin, remapMax, colorAnim, colorRemapMin, colorRemapMax, unscaledTime));
    }

    private IEnumerator PlayVignetteRoutine(
        Color targetColor,
        AnimationCurve intensityAnim, float duration, float amplitude, float remapMin, float remapMax,
        AnimationCurve colorAnim, float colorRemapMin, float colorRemapMax,
        bool unscaled)
    {
        float start = unscaled ? Time.unscaledTime : Time.time;
        float end = start + Mathf.Max(0.0001f, duration);

        var intensityParam = GetIntensityParameter(EffectOverride);
        var colorParam = EffectOverride.color;

        while ((unscaled ? Time.unscaledTime : Time.time) < end)
        {
            float now = unscaled ? Time.unscaledTime : Time.time;
            float t = Mathf.InverseLerp(start, end, now);

            float ic = Mathf.Clamp01(intensityAnim.Evaluate(t));
            float iMapped = Mathf.Lerp(remapMin, remapMax, ic) * amplitude;
            float iFinal = relativeIntensity ? ClampValue(InitialIntensity + iMapped) : ClampValue(iMapped);
            intensityParam.value = iFinal;
            LastEvaluatedIntensity = iFinal;

            if (animateColor)
            {
                float cc = Mathf.Clamp01(colorAnim.Evaluate(t));
                float cMapped = Mathf.Lerp(colorRemapMin, colorRemapMax, cc);
                colorParam.value = Color.Lerp(_initialColor, targetColor, cMapped);
            }

            yield return null;
        }

        float endMapped = ClampValue(remapMax * amplitude);
        float endFinal = relativeIntensity ? ClampValue(InitialIntensity + endMapped) : endMapped;
        float final = MapFinalValue(true, endFinal, endMapped);
        intensityParam.value = final;
        LastEvaluatedIntensity = final;

        if (resetToInitialOnStop) colorParam.value = _initialColor;

        _vignetteRoutine = null;
        Log("Play finished");
    }
}