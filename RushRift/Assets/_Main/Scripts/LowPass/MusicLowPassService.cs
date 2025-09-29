using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    public class MusicLowPassService : MonoBehaviour
    {
        [Header("Mixer Binding")]
        [SerializeField, Tooltip("AudioMixer that contains the low-pass exposed parameter.")]
        private AudioMixer boundAudioMixer;
        [SerializeField, Tooltip("Exposed parameter name controlling the low-pass cutoff in Hz.")]
        private string boundExposedParameterName = "MusicLowpassHz";

        [Header("Defaults")]
        [SerializeField, Tooltip("Default unpaused cutoff (Hz).")]
        private float defaultUnpausedCutoffHz = 5000f;
        [SerializeField, Tooltip("Use unscaled time for ramps.")]
        private bool useUnscaledTimeForRamps = true;

        [Header("Scene Hooks")]
        [SerializeField, Tooltip("If enabled, resets the cutoff to unpaused on scene loaded.")]
        private bool resetToUnpausedOnSceneLoaded = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logs.")]
        private bool isDebugLoggingEnabled = false;

        private static MusicLowPassService _instance;
        private static bool s_isQuitting;

        private Coroutine _rampCoroutine;
        private float _unpausedCutoffHz;
        private bool _sceneHookInstalled;

        public static void Configure(AudioMixer mixer, string exposedParam, float unpausedHz, bool useUnscaled, bool resetOnSceneLoaded)
        {
            var s = EnsureInstance();
            if (!s) return;
            s.boundAudioMixer = mixer;
            s.boundExposedParameterName = exposedParam;
            s._unpausedCutoffHz = Mathf.Max(10f, unpausedHz);
            s.useUnscaledTimeForRamps = useUnscaled;
            s.resetToUnpausedOnSceneLoaded = resetOnSceneLoaded;
            s.InstallSceneHookIfNeeded();
            s.Log($"Configured | param={exposedParam} unpaused={s._unpausedCutoffHz:0.##}Hz unscaled={useUnscaled} resetOnLoad={resetOnSceneLoaded}");
        }

        public static void SetDebugLogging(bool enabled) { var s = TryGetInstance(); if (s) s.isDebugLoggingEnabled = enabled; }
        public static void SetUnpausedCutoffHz(float hz) { var s = TryGetInstance(); if (!s) return; s._unpausedCutoffHz = Mathf.Max(10f, hz); s.Log($"Unpaused cutoff set → {s._unpausedCutoffHz:0.##}Hz"); }
        public static void SetCutoffImmediate(float hz) { var s = TryGetInstance(); if (!s || !s.Validate()) return; s.boundAudioMixer.SetFloat(s.boundExposedParameterName, Mathf.Max(10f, hz)); s.Log($"Immediate cutoff → {hz:0.##}Hz"); }
        public static void RampTo(float targetHz, float durationSeconds) { var s = TryGetInstance(); if (!s || !s.Validate()) return; if (s._rampCoroutine != null) s.StopCoroutine(s._rampCoroutine); s._rampCoroutine = s.StartCoroutine(s.RampRoutine(Mathf.Max(10f, targetHz), Mathf.Max(0f, durationSeconds))); s.Log($"RampTo | target={targetHz:0.##}Hz dur={durationSeconds:0.###}s"); }
        public static void ApplyPaused(float pausedHz, float rampSeconds) { RampTo(Mathf.Max(10f, pausedHz), Mathf.Max(0f, rampSeconds)); }
        public static void ClearToUnpaused(float rampSeconds) { var s = TryGetInstance(); if (!s) return; RampTo(s._unpausedCutoffHz > 0f ? s._unpausedCutoffHz : s.defaultUnpausedCutoffHz, Mathf.Max(0f, rampSeconds)); }
        public static void SetPaused(bool isPaused, float pausedHz, float pauseRampSeconds, float resumeRampSeconds) { if (isPaused) ApplyPaused(pausedHz, pauseRampSeconds); else ClearToUnpaused(resumeRampSeconds); }

        private static MusicLowPassService TryGetInstance()
        {
            if (_instance) return _instance;
            if (s_isQuitting) return null;
            if (!Application.isPlaying) return null;

            var found = FindObjectOfType<MusicLowPassService>(true);
            if (found) { _instance = found; return _instance; }
            return null;
        }

        private static MusicLowPassService EnsureInstance()
        {
            if (_instance) return _instance;
            if (s_isQuitting) return null;
            if (!Application.isPlaying) return null;

            var found = FindObjectOfType<MusicLowPassService>(true);
            if (found) { _instance = found; return _instance; }

            var go = new GameObject("_MusicLowPassService");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MusicLowPassService>();
            return _instance;
        }

        private IEnumerator RampRoutine(float targetHz, float duration)
        {
            float current;
            if (!boundAudioMixer.GetFloat(boundExposedParameterName, out current)) current = _unpausedCutoffHz > 0f ? _unpausedCutoffHz : defaultUnpausedCutoffHz;

            if (duration <= 0f)
            {
                boundAudioMixer.SetFloat(boundExposedParameterName, targetHz);
                _rampCoroutine = null;
                yield break;
            }

            float t0 = useUnscaledTimeForRamps ? Time.unscaledTime : Time.time;
            float t1 = t0 + duration;
            while ((useUnscaledTimeForRamps ? Time.unscaledTime : Time.time) < t1)
            {
                float now = useUnscaledTimeForRamps ? Time.unscaledTime : Time.time;
                float t = Mathf.InverseLerp(t0, t1, now);
                float v = Mathf.Lerp(current, targetHz, Mathf.SmoothStep(0f, 1f, t));
                boundAudioMixer.SetFloat(boundExposedParameterName, v);
                yield return null;
            }

            boundAudioMixer.SetFloat(boundExposedParameterName, targetHz);
            _rampCoroutine = null;
        }

        private void Awake()
        {
            if (_instance && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            if (!transform.parent) DontDestroyOnLoad(gameObject);
            _unpausedCutoffHz = defaultUnpausedCutoffHz;
            InstallSceneHookIfNeeded();
        }

        private void OnApplicationQuit()
        {
            s_isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_rampCoroutine != null) { StopCoroutine(_rampCoroutine); _rampCoroutine = null; }
            if (_sceneHookInstalled) SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_instance == this) _instance = null;
        }

        private void InstallSceneHookIfNeeded()
        {
            if (_sceneHookInstalled || !resetToUnpausedOnSceneLoaded) return;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            _sceneHookInstalled = true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Validate()) return;
            boundAudioMixer.SetFloat(boundExposedParameterName, _unpausedCutoffHz > 0f ? _unpausedCutoffHz : defaultUnpausedCutoffHz);
            Log($"SceneLoaded reset → {_unpausedCutoffHz:0.##}Hz");
        }

        private bool Validate()
        {
            if (!boundAudioMixer) { Log("Missing AudioMixer"); return false; }
            if (string.IsNullOrEmpty(boundExposedParameterName)) { Log("Missing exposed parameter"); return false; }
            return true;
        }

        private void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[MusicLowPassService] {msg}", this);
        }
    }
}