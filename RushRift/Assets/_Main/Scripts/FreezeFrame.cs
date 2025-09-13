using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-32000)]
[DisallowMultipleComponent]
public class FreezeFrame : MonoBehaviour
{
    private static FreezeFrame _instance;

    [Header("Settings")]
    [SerializeField, Tooltip("If enabled, the freeze frame system persists across scene loads.")]
    private bool persistAcrossScenes = true;

    [SerializeField, Tooltip("If enabled, ignores freeze requests when the game is paused via PauseEventBus.")]
    private bool respectGlobalPause = true;

    [SerializeField, Tooltip("Default duration of the freeze in seconds, measured in unscaled time.")]
    private float defaultFreezeDurationSeconds = 0.02f;

    [SerializeField, Tooltip("Timescale applied during the freeze.")]
    private float frozenTimeScale = 0f;

    [SerializeField, Tooltip("If the current Time.timeScale is below this value, freeze requests are ignored.")]
    private float minimumTimescaleThreshold = 0.1f;

    [SerializeField, Tooltip("Seconds to ramp back to normal timescale after the freeze.")]
    private float restoreRampSeconds = 0.08f;

    [Header("Input")]
    [SerializeField, Tooltip("Optional key to trigger a test freeze at runtime.")]
    private KeyCode testKey = KeyCode.None;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw gizmos while frozen.")]
    private bool drawGizmos = true;

    private float _originalTimeScale = 1f;
    private float _originalFixedDeltaTime = 0.02f;
    private float _freezeEndUnscaledTime;
    private bool _isFrozen;
    private Coroutine _freezeRoutine;

    public static FreezeFrame Instance
    {
        get
        {
            if (_instance) return _instance;
            var go = new GameObject(nameof(FreezeFrame));
            _instance = go.AddComponent<FreezeFrame>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _originalTimeScale = Time.timeScale;
        _originalFixedDeltaTime = Time.fixedDeltaTime;

        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (_isFrozen) RestoreInstant();
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
            Trigger(defaultFreezeDurationSeconds);

        if (!_isFrozen) return;

        if (Time.unscaledTime >= _freezeEndUnscaledTime)
        {
            if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
            _freezeRoutine = StartCoroutine(RestoreRamp(restoreRampSeconds));
        }
    }

    public static bool Trigger(float durationSeconds) =>
        Instance.InternalTrigger(durationSeconds, Instance.restoreRampSeconds);

    public static bool Trigger(float durationSeconds, float restoreSeconds) =>
        Instance.InternalTrigger(durationSeconds, restoreSeconds);

    public static bool TriggerDefault() =>
        Instance.InternalTrigger(Instance.defaultFreezeDurationSeconds, Instance.restoreRampSeconds);

    private bool InternalTrigger(float durationSeconds, float restoreSeconds)
    {
        if (respectGlobalPause && typeof(PauseEventBus) != null && PauseEventBus.IsPaused)
        {
            Log("Ignored: paused");
            return false;
        }

        if (Time.timeScale < minimumTimescaleThreshold)
        {
            Log("Ignored: timescale below threshold");
            return false;
        }

        if (!_isFrozen)
        {
            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            ApplyTimeScale(frozenTimeScale);
            _isFrozen = true;
            Log($"Freeze start for {durationSeconds:0.###}s");
        }
        else
        {
            Log($"Freeze extended by {durationSeconds:0.###}s");
        }

        _freezeEndUnscaledTime = Mathf.Max(_freezeEndUnscaledTime, Time.unscaledTime + Mathf.Max(0f, durationSeconds));

        if (_freezeRoutine != null)
        {
            StopCoroutine(_freezeRoutine);
            _freezeRoutine = null;
        }

        return true;
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (_isFrozen)
        {
            RestoreInstant();
            Log("Restored on scene load");
        }
    }

    private void ApplyTimeScale(float ts)
    {
        Time.timeScale = Mathf.Max(0f, ts);
        Time.fixedDeltaTime = _originalFixedDeltaTime * Mathf.Max(0.0001f, Time.timeScale);
    }

    private void RestoreInstant()
    {
        _isFrozen = false;
        ApplyTimeScale(_originalTimeScale);
    }

    private IEnumerator RestoreRamp(float seconds)
    {
        _isFrozen = false;

        float startTS = Time.timeScale;
        float targetTS = Mathf.Max(0.0001f, _originalTimeScale);
        float startFixed = _originalFixedDeltaTime * Mathf.Max(0.0001f, startTS);
        float targetFixed = _originalFixedDeltaTime * Mathf.Max(0.0001f, targetTS);

        if (seconds <= 0f)
        {
            ApplyTimeScale(targetTS);
            Log("Restore instant");
            _freezeRoutine = null;
            yield break;
        }

        float t0 = Time.unscaledTime;
        float t1 = t0 + seconds;
        while (Time.unscaledTime < t1)
        {
            float t = Mathf.InverseLerp(t0, t1, Time.unscaledTime);
            float k = Mathf.SmoothStep(0f, 1f, t);
            Time.timeScale = Mathf.Lerp(startTS, targetTS, k);
            Time.fixedDeltaTime = Mathf.Lerp(startFixed, targetFixed, k);
            yield return null;
        }

        ApplyTimeScale(targetTS);
        Log("Restore complete");
        _freezeRoutine = null;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FreezeFrame] {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos || !_isFrozen) return;
        Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.35f);
        var cam = Camera.main;
        Vector3 pos = cam ? cam.transform.position + cam.transform.forward * 1.5f : transform.position;
        Gizmos.DrawSphere(pos, 0.2f);
        Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(pos, 0.2f);
    }
#endif
}
