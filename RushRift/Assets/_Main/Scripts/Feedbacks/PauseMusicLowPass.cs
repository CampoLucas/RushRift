using UnityEngine;
using UnityEngine.Audio;

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
        [SerializeField, Tooltip("If enabled, reads the current mixer value on Start when it’s not paused and uses it as unpaused.")]
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
        [SerializeField, Tooltip("Force-reset to unpaused right after a new scene finishes loading.")]
        private bool resetOnSceneLoaded = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logs.")]
        private bool isDebugLoggingEnabled = false;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            MusicLowPassService.Configure(targetAudioMixer, exposedParameterName, unpausedCutoffHz, useUnscaledTime, resetOnSceneLoaded);
            MusicLowPassService.SetDebugLogging(isDebugLoggingEnabled);
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            if (!targetAudioMixer || string.IsNullOrEmpty(exposedParameterName)) return;

            if (captureUnpausedFromMixerOnStart && targetAudioMixer.GetFloat(exposedParameterName, out var v))
            {
                float pausedLikeThreshold = pausedCutoffHz + 25f;
                if (v > pausedLikeThreshold)
                {
                    unpausedCutoffHz = v;
                    MusicLowPassService.SetUnpausedCutoffHz(unpausedCutoffHz);
                }
                else
                {
                    MusicLowPassService.SetCutoffImmediate(unpausedCutoffHz);
                }
            }
            else
            {
                MusicLowPassService.SetCutoffImmediate(unpausedCutoffHz);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (applyOnEnableAndClearOnDisable)
                MusicLowPassService.ApplyPaused(pausedCutoffHz, pauseRampSeconds);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (applyOnEnableAndClearOnDisable)
                MusicLowPassService.ClearToUnpaused(resumeRampSeconds);
        }

        public void ApplyPauseFilter()
        {
            if (!Application.isPlaying) return;
            MusicLowPassService.ApplyPaused(pausedCutoffHz, pauseRampSeconds);
        }

        public void ClearPauseFilter()
        {
            if (!Application.isPlaying) return;
            MusicLowPassService.ClearToUnpaused(resumeRampSeconds);
        }

        public void SetPaused(bool isPaused)
        {
            if (!Application.isPlaying) return;
            MusicLowPassService.SetPaused(isPaused, pausedCutoffHz, pauseRampSeconds, resumeRampSeconds);
        }
    }
}