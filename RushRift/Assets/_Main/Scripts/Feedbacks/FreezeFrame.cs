using System.Collections;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Feedbacks
{
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public class FreezeFrame : MonoBehaviour
    {
//         private static FreezeFrame _instance;
//
//         [Header("Settings")]
//         [SerializeField, Tooltip("Default duration of the freeze in seconds, measured in unscaled time.")]
//         private float defaultFreezeDurationSeconds = 0.02f;
//
//         [SerializeField, Tooltip("Timescale applied during the freeze.")]
//         private float frozenTimeScale = 0f;
//
//         [SerializeField, Tooltip("If the current Time.timeScale is below this value, freeze requests are ignored.")]
//         private float minimumTimescaleThreshold = 0.1f;
//
//         [SerializeField, Tooltip("Seconds to ramp back to normal timescale after the freeze.")]
//         private float restoreRampSeconds = 0.08f;
//
//         [Header("Input")]
//         [SerializeField, Tooltip("Optional key to trigger a test freeze at runtime.")]
//         private KeyCode testKey = KeyCode.None;
//
//         [Header("Debug")]
//         [SerializeField, Tooltip("If enabled, prints detailed logs.")]
//         private bool isDebugLoggingEnabled = false;
//
//         [SerializeField, Tooltip("Draw gizmos while frozen.")]
//         private bool drawGizmos = true;
//
//         private float _originalTimeScale = 1f;
//         private float _originalFixedDeltaTime = 0.02f;
//         private float _freezeEndUnscaledTime;
//         private bool _isFrozen;
//         private Coroutine _freezeRoutine;
//
//         public static FreezeFrame Instance
//         {
//             get
//             {
//                 if (_instance) return _instance;
//                 var go = new GameObject(nameof(FreezeFrame));
//                 _instance = go.AddComponent<FreezeFrame>();
//                 return _instance;
//             }
//         }
//
// #if UNITY_EDITOR
//         private void OnGUI()
//         {
//             var style = new GUIStyle(GUI.skin.label)
//             {
//                 fontSize = 16,
//                 alignment = TextAnchor.UpperLeft
//             };
//
// #if UNITY_EDITOR
//             float y = 200f;
// #else
//             float y = 10f;
// #endif
//
//             // TimeScale
//             bool tsOk = Mathf.Approximately(Time.timeScale, 1f);
//             style.normal.textColor = tsOk ? Color.green : Color.red;
//             GUI.Label(new Rect(10, y, 500, 30), $"timeScale: {Time.timeScale:0.000}", style);
//             y += 22;
//
//             // fixedDeltaTime
//             style.normal.textColor = Color.white;
//             GUI.Label(new Rect(10, y, 500, 30), $"fixedDeltaTime: {Time.fixedDeltaTime:0.000000}", style);
//             y += 22;
//
//             // Frozen state
//             style.normal.textColor = _isFrozen ? Color.red : Color.green;
//             GUI.Label(new Rect(10, y, 500, 30), $"isFrozen: {_isFrozen}", style);
//             y += 22;
//
//             // Freeze End Time
//             style.normal.textColor = Color.white;
//             GUI.Label(new Rect(10, y, 500, 30), $"freezeEndUnscaledTime: {_freezeEndUnscaledTime:0.000}", style);
//             y += 22;
//
//             // Time Remaining
//             float remaining = Mathf.Max(0f, _freezeEndUnscaledTime - Time.unscaledTime);
//             GUI.Label(new Rect(10, y, 500, 30), $"freezeRemaining: {remaining:0.000}", style);
//             y += 22;
//
//             // Routine info
//             GUI.Label(new Rect(10, y, 500, 30), $"restoreRoutine: {(_freezeRoutine != null ? "running" : "null")}", style);
//             y += 22;
//
//             // Original times
//             GUI.Label(new Rect(10, y, 500, 30), $"originalTS: {_originalTimeScale}", style);
//             y += 22;
//             GUI.Label(new Rect(10, y, 500, 30), $"originalFixed: {_originalFixedDeltaTime}", style);
//         }
// #endif
//         private void Awake()
//         {
//             if (_instance && _instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//
//             _instance = this;
//
//             _originalTimeScale = Time.timeScale;
//             _originalFixedDeltaTime = Time.fixedDeltaTime;
//
//             
//             SceneManager.sceneLoaded += HandleSceneLoaded;
//         }
//
//         private void OnDestroy()
//         {
//             SceneManager.sceneLoaded -= HandleSceneLoaded;
//             if (_isFrozen) RestoreInstant();
//             if (_instance == this) _instance = null;
//         }
//
//         private void Update()
//         {
//             // if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
//             //     Trigger(defaultFreezeDurationSeconds);
//
//             
//             // // WATCHDOG
//             // if (Time.timeScale < 0.95f && !_isFrozen)
//             // {
//             //     Debug.LogWarning("[FreezeFrame] Global watchdog: abnormal timeScale detected, restoring.");
//             //     Time.timeScale = 1f;
//             //     Time.fixedDeltaTime = _originalFixedDeltaTime;
//             // }
//             // if (_freezeRoutine == null && !_isFrozen)
//             // {
//             //     if (Time.timeScale != 1f)
//             //     {
//             //         Debug.LogWarning("[FreezeFrame] Inconsistent timescale after freezing. Restoring.");
//             //         Time.timeScale = 1f;
//             //         Time.fixedDeltaTime = _originalFixedDeltaTime;
//             //     }
//             // }
//             // if (!_isFrozen) return;
//             //
//             // if (Time.unscaledTime >= _freezeEndUnscaledTime)
//             // {
//             //     if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
//             //     _freezeRoutine = StartCoroutine(RestoreRamp(restoreRampSeconds));
//             // }
//         }
//
//         public static bool Trigger(float durationSeconds) => Instance && Instance.InternalTrigger(durationSeconds, Instance.restoreRampSeconds);
//
//         public static bool Trigger(float durationSeconds, float restoreSeconds) => Instance && 
//             Instance.InternalTrigger(durationSeconds, restoreSeconds);
//
//         public static bool TriggerDefault() => Instance && Instance.InternalTrigger(Instance.defaultFreezeDurationSeconds, Instance.restoreRampSeconds);
//
//         private bool InternalTrigger(float durationSeconds, float restoreSeconds)
//         {
//             return false;
//             // if (PauseHandler.IsPaused)
//             // {
//             //     Log("Ignored: paused");
//             //     return false;
//             // }
//             //
//             // if (Time.timeScale < minimumTimescaleThreshold)
//             // {
//             //     Log("Ignored: timescale below threshold");
//             //     return false;
//             // }
//             //
//             // if (!_isFrozen)
//             // {
//             //     _originalTimeScale = Time.timeScale;
//             //     _originalFixedDeltaTime = Time.fixedDeltaTime;
//             //     ApplyTimeScale(frozenTimeScale);
//             //     _isFrozen = true;
//             //     Log($"Freeze start for {durationSeconds:0.###}s");
//             // }
//             // else
//             // {
//             //     Log($"Freeze extended by {durationSeconds:0.###}s");
//             // }
//             //
//             // _freezeEndUnscaledTime = Mathf.Max(_freezeEndUnscaledTime, Time.unscaledTime + Mathf.Max(0f, durationSeconds));
//             //
//             // if (_freezeRoutine != null)
//             // {
//             //     StopCoroutine(_freezeRoutine);
//             //     _freezeRoutine = null;
//             // }
//             //
//             // return true;
//         }
//
//         private void HandleSceneLoaded(Scene s, LoadSceneMode m)
//         {
//             if (_isFrozen)
//             {
//                 RestoreInstant();
//                 Log("Restored on scene load");
//             }
//         }
//
//         private void ApplyTimeScale(float ts)
//         {
//             // Time.timeScale = Mathf.Max(0f, ts);
//             // Time.fixedDeltaTime = _originalFixedDeltaTime * Mathf.Max(0.0001f, Time.timeScale);
//         }
//
//         private void RestoreInstant()
//         {
//             _isFrozen = false;
//             ApplyTimeScale(_originalTimeScale);
//         }
//
//         // private IEnumerator RestoreRamp(float seconds)
//         // {
//         //     _isFrozen = false;
//         //
//         //     float startTS = Time.timeScale;
//         //     float targetTS = Mathf.Max(0.0001f, _originalTimeScale);
//         //     float startFixed = _originalFixedDeltaTime * Mathf.Max(0.0001f, startTS);
//         //     float targetFixed = _originalFixedDeltaTime * Mathf.Max(0.0001f, targetTS);
//         //
//         //     if (seconds <= 0f)
//         //     {
//         //         ApplyTimeScale(targetTS);
//         //         Log("Restore instant");
//         //         _freezeRoutine = null;
//         //         yield break;
//         //     }
//         //
//         //     float t0 = Time.unscaledTime;
//         //     float t1 = t0 + seconds;
//         //     while (Time.unscaledTime < t1)
//         //     {
//         //         float t = Mathf.InverseLerp(t0, t1, Time.unscaledTime);
//         //         float k = Mathf.SmoothStep(0f, 1f, t);
//         //         Time.timeScale = Mathf.Lerp(startTS, targetTS, k);
//         //         Time.fixedDeltaTime = Mathf.Lerp(startFixed, targetFixed, k);
//         //         yield return null;
//         //     }
//         //
//         //     ApplyTimeScale(targetTS);
//         //     Log("Restore complete");
//         //     _freezeRoutine = null;
//         // }
//
//         private void Log(string msg)
//         {
//             if (!isDebugLoggingEnabled) return;
//             Debug.Log($"[FreezeFrame] {msg}", this);
//         }
//
// #if UNITY_EDITOR
//         private void OnDrawGizmos()
//         {
//             if (!drawGizmos || !_isFrozen) return;
//             Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.35f);
//             var cam = Camera.main;
//             Vector3 pos = cam ? cam.transform.position + cam.transform.forward * 1.5f : transform.position;
//             Gizmos.DrawSphere(pos, 0.2f);
//             Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.9f);
//             Gizmos.DrawWireSphere(pos, 0.2f);
//         }
// #endif
    }
}
