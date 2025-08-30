using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class PauseMusicLowPass : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField, Tooltip("AudioMixer that contains the Music channel low-pass parameter.")]
    private AudioMixer targetAudioMixer;
    [SerializeField, Tooltip("Exposed parameter name controlling the Music low-pass cutoff in Hz.")]
    private string exposedParameterName = "MusicLowpassHz";

    [Header("Cutoff Targets (Hz)")]
    [SerializeField, Tooltip("Cutoff while paused.")]
    private float pausedCutoffHz = 250f;
    [SerializeField, Tooltip("Cutoff when unpaused.")]
    private float unpausedCutoffHz = 5000f;
    [SerializeField, Tooltip("If enabled, reads the current mixer value on Start and uses it as the unpaused target.")]
    private bool captureUnpausedFromMixerOnStart = true;

    [Header("Ramps")]
    [SerializeField, Tooltip("Seconds to ramp into pause cutoff.")]
    private float pauseRampSeconds = 0.05f;
    [SerializeField, Tooltip("Seconds to ramp back to unpaused cutoff.")]
    private float resumeRampSeconds = 1.25f;
    [SerializeField, Tooltip("Use unscaled time for ramps.")]
    private bool useUnscaledTime = true;

    [Header("Automatic Hooks")]
    [SerializeField, Tooltip("Apply pause cutoff in OnEnable and resume in OnDisable.")]
    private bool applyOnEnableAndClearOnDisable = false;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    private Coroutine rampCoroutine;

    private void Start()
    {
        if (captureUnpausedFromMixerOnStart && targetAudioMixer && !string.IsNullOrEmpty(exposedParameterName))
        {
            if (targetAudioMixer.GetFloat(exposedParameterName, out var v))
            {
                unpausedCutoffHz = v;
                Log($"Captured unpaused cutoff = {unpausedCutoffHz:0.##} Hz");
            }
        }
    }

    private void OnEnable()
    {
        if (applyOnEnableAndClearOnDisable) ApplyPauseFilter();
    }

    private void OnDisable()
    {
        if (applyOnEnableAndClearOnDisable) ClearPauseFilter();
    }

    public void ApplyPauseFilter()
    {
        if (!Validate()) return;
        StartRamp(pausedCutoffHz, pauseRampSeconds);
        Log($"ApplyPauseFilter → {pausedCutoffHz:0.##} Hz");
    }

    public void ClearPauseFilter()
    {
        if (!Validate()) return;
        StartRamp(unpausedCutoffHz, resumeRampSeconds);
        Log($"ClearPauseFilter → {unpausedCutoffHz:0.##} Hz");
    }

    public void SetPaused(bool isPaused)
    {
        if (isPaused) ApplyPauseFilter();
        else ClearPauseFilter();
    }

    private void StartRamp(float targetHz, float duration)
    {
        if (rampCoroutine != null) StopCoroutine(rampCoroutine);
        rampCoroutine = StartCoroutine(RampTo(targetHz, Mathf.Max(0f, duration)));
    }

    private IEnumerator RampTo(float targetHz, float duration)
    {
        if (!targetAudioMixer.GetFloat(exposedParameterName, out var current)) current = unpausedCutoffHz;
        if (duration <= 0f)
        {
            targetAudioMixer.SetFloat(exposedParameterName, targetHz);
            rampCoroutine = null;
            yield break;
        }

        float t0 = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t1 = t0 + duration;
        while ((useUnscaledTime ? Time.unscaledTime : Time.time) < t1)
        {
            float t = Mathf.InverseLerp(t0, t1, useUnscaledTime ? Time.unscaledTime : Time.time);
            float v = Mathf.Lerp(current, targetHz, Mathf.SmoothStep(0f, 1f, t));
            targetAudioMixer.SetFloat(exposedParameterName, v);
            yield return null;
        }
        targetAudioMixer.SetFloat(exposedParameterName, targetHz);
        rampCoroutine = null;
    }

    private bool Validate()
    {
        if (!targetAudioMixer) { Log("Missing AudioMixer"); return false; }
        if (string.IsNullOrEmpty(exposedParameterName)) { Log("Missing exposed parameter name"); return false; }
        return true;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[PauseMusicLowPass] {name}: {msg}", this);
    }
}
