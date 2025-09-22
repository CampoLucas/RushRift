using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Feedbacks
{
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
        [SerializeField, Tooltip("If enabled, reads the current mixer value on Start and uses it as the unpaused target when it looks like a normal value (not a paused one).")]
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
        [SerializeField, Tooltip("Force-reset to unpaused when this component is destroyed or scene unloads.")]
        private bool resetOnDestroyOrSceneUnload = true;
        [SerializeField, Tooltip("Force-reset to unpaused right after a new scene finishes loading.")]
        private bool resetOnSceneLoaded = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        private bool isDebugLoggingEnabled = false;

        private Coroutine rampCoroutine;

        private static bool sceneHookInstalled;
        private static AudioMixer lastKnownMixer;
        private static string lastKnownParam;
        private static float lastKnownUnpausedHz;

        private void Awake()
        {
            if (targetAudioMixer && !string.IsNullOrEmpty(exposedParameterName))
            {
                lastKnownMixer = targetAudioMixer;
                lastKnownParam = exposedParameterName;
                lastKnownUnpausedHz = unpausedCutoffHz;
            }

            if (resetOnSceneLoaded && !sceneHookInstalled)
            {
                SceneManager.sceneLoaded += HandleSceneLoadedReset;
                sceneHookInstalled = true;
            }
        }

        private void Start()
        {
            if (captureUnpausedFromMixerOnStart && targetAudioMixer && !string.IsNullOrEmpty(exposedParameterName))
            {
                if (targetAudioMixer.GetFloat(exposedParameterName, out var v))
                {
                    float pausedLikeThreshold = pausedCutoffHz + 25f;
                    if (v > pausedLikeThreshold)
                    {
                        unpausedCutoffHz = v;
                        lastKnownUnpausedHz = unpausedCutoffHz;
                        Log($"Captured unpaused cutoff = {unpausedCutoffHz:0.##} Hz");
                    }
                    else
                    {
                        targetAudioMixer.SetFloat(exposedParameterName, unpausedCutoffHz);
                        Log($"Mixer looked paused ({v:0.##} Hz). Reset to {unpausedCutoffHz:0.##} Hz.");
                    }
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

        private void OnDestroy()
        {
            if (rampCoroutine != null) StopCoroutine(rampCoroutine);
            if (resetOnDestroyOrSceneUnload) ForceResetToUnpaused();
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
                float now = useUnscaledTime ? Time.unscaledTime : Time.time;
                float t = Mathf.InverseLerp(t0, t1, now);
                float v = Mathf.Lerp(current, targetHz, Mathf.SmoothStep(0f, 1f, t));
                targetAudioMixer.SetFloat(exposedParameterName, v);
                yield return null;
            }
            targetAudioMixer.SetFloat(exposedParameterName, targetHz);
            rampCoroutine = null;
        }

        private void ForceResetToUnpaused()
        {
            if (!targetAudioMixer || string.IsNullOrEmpty(exposedParameterName)) return;
            targetAudioMixer.SetFloat(exposedParameterName, unpausedCutoffHz);
            Log($"Force reset to {unpausedCutoffHz:0.##} Hz");
        }

        private static void HandleSceneLoadedReset(Scene scene, LoadSceneMode mode)
        {
            if (!lastKnownMixer || string.IsNullOrEmpty(lastKnownParam)) return;
            lastKnownMixer.SetFloat(lastKnownParam, lastKnownUnpausedHz > 0f ? lastKnownUnpausedHz : 5000f);
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
}
