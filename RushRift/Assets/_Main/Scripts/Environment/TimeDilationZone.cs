using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class TimeDilationZone : MonoBehaviour
{
    [Header("Zone Setup")]
    [SerializeField, Tooltip("Objects with this tag will activate the time dilation. Leave empty to accept any.")]
    private string requiredActivatorTag = "Player";
    [SerializeField, Tooltip("Automatically sets the BoxCollider to be a Trigger on validate.")]
    private bool autoConfigureColliderAsTrigger = true;

    [Header("Time Dilation")]
    [SerializeField, Tooltip("Global Time.timeScale while a qualifying object is inside. 1 = normal, 0.25 = quarter speed.")]
    [Range(0f, 1f)] private float targetTimeScaleWhileInside = 0.3f;
    [SerializeField, Tooltip("Blend time (seconds) when entering the zone.")]
    private float enterBlendDurationSeconds = 0.10f;
    [SerializeField, Tooltip("Blend time (seconds) when exiting the zone.")]
    private float exitBlendDurationSeconds = 0.12f;
    [SerializeField, Tooltip("Scale Time.fixedDeltaTime along with Time.timeScale for consistent physics.")]
    private bool scaleFixedDeltaTime = true;

    [Header("Audio Low-Pass")]
    [SerializeField, Tooltip("Apply a low-pass filter while inside the zone.")]
    private bool applyLowPassFilter = true;
    [SerializeField, Tooltip("AudioMixer that owns the exposed cutoff parameter.")]
    private AudioMixer targetAudioMixer;
    [SerializeField, Tooltip("Exposed parameter name for cutoff frequency in Hz.")]
    private string exposedLowPassParameterName = "MusicLowpassHz";
    [SerializeField, Tooltip("Cutoff in Hz while inside the zone.")]
    private float insideCutoffHz = 1000f;
    [SerializeField, Tooltip("Cutoff in Hz when no zones are active.")]
    private float outsideCutoffHz = 5000f;
    [SerializeField, Tooltip("Capture the current mixer value on Start as the outside cutoff.")]
    private bool captureOutsideCutoffFromMixerOnStart = true;
    [SerializeField, Tooltip("Seconds to ramp into the inside cutoff.")]
    private float enterLowPassRampSeconds = 0.15f;
    [SerializeField, Tooltip("Seconds to ramp back to the outside cutoff.")]
    private float exitLowPassRampSeconds = 0.35f;
    [SerializeField, Tooltip("Use unscaled time for audio ramps.")]
    private bool useUnscaledTimeForAudio = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs and draws gizmos.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draws the zone bounds in the scene view.")]
    private bool drawGizmos = true;

    private BoxCollider zoneCollider;
    private int currentQualifiedOccupantCount;

    private static readonly HashSet<TimeDilationZone> ActiveZones = new();
    private static Coroutine timeBlendRoutine;
    private static MonoBehaviour timeBlendHost;
    private static float defaultFixedDeltaTime = -1f;

    private static Coroutine audioBlendRoutine;
    private static MonoBehaviour audioBlendHost;
    private static AudioMixer activeAudioMixer;
    private static string activeAudioParam;
    private static float globalOutsideCutoffHz = -1f;
    private static bool sceneHooksRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSceneHooks()
    {
        if (sceneHooksRegistered) return;
        sceneHooksRegistered = true;
        SceneManager.activeSceneChanged += (_, __) => ForceResetEffectsImmediate(true);
        SceneManager.sceneLoaded += (_, __) =>
        {
            if (ActiveZones.Count == 0) ForceResetEffectsImmediate(false);
        };
    }

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
        if (defaultFixedDeltaTime < 0f) defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (applyLowPassFilter && captureOutsideCutoffFromMixerOnStart && targetAudioMixer && !string.IsNullOrEmpty(exposedLowPassParameterName))
        {
            if (targetAudioMixer.GetFloat(exposedLowPassParameterName, out var v)) globalOutsideCutoffHz = v;
        }
        if (globalOutsideCutoffHz < 0f) globalOutsideCutoffHz = outsideCutoffHz;
    }

    private void OnValidate()
    {
        zoneCollider = GetComponent<BoxCollider>();
        if (autoConfigureColliderAsTrigger && zoneCollider) zoneCollider.isTrigger = true;
        targetTimeScaleWhileInside = Mathf.Clamp01(targetTimeScaleWhileInside);
        enterBlendDurationSeconds = Mathf.Max(0f, enterBlendDurationSeconds);
        exitBlendDurationSeconds = Mathf.Max(0f, exitBlendDurationSeconds);
        insideCutoffHz = Mathf.Max(10f, insideCutoffHz);
        outsideCutoffHz = Mathf.Max(10f, outsideCutoffHz);
        enterLowPassRampSeconds = Mathf.Max(0f, enterLowPassRampSeconds);
        exitLowPassRampSeconds = Mathf.Max(0f, exitLowPassRampSeconds);
    }

    private void OnEnable()
    {
        RebuildOccupantsFromOverlap();
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
        if (ActiveZones.Count == 0) ForceResetEffectsImmediate(false);
        else
        {
            RefreshGlobalTimeScale(false);
            RefreshGlobalLowPass(false);
        }
    }

    private void OnDestroy()
    {
        ActiveZones.Remove(this);
        if (ActiveZones.Count == 0) ForceResetEffectsImmediate(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActivator(other)) return;

        currentQualifiedOccupantCount++;
        if (currentQualifiedOccupantCount == 1)
        {
            ActiveZones.Add(this);
            RefreshGlobalTimeScale(true);
            RefreshGlobalLowPass(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsActivator(other)) return;

        currentQualifiedOccupantCount = Mathf.Max(0, currentQualifiedOccupantCount - 1);
        if (currentQualifiedOccupantCount == 0)
        {
            ActiveZones.Remove(this);
            if (ActiveZones.Count == 0) ForceResetEffectsImmediate(false);
            else
            {
                RefreshGlobalTimeScale(false);
                RefreshGlobalLowPass(false);
            }
        }
    }

    private bool IsActivator(Collider other)
    {
        if (!other) return false;
        if (string.IsNullOrEmpty(requiredActivatorTag)) return true;
        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        bool ok = root.CompareTag(requiredActivatorTag);
        if (isDebugLoggingEnabled && !ok)
            Debug.Log($"[TimeDilationZone] {name}: Ignored {root.name} (tag={root.tag}, required={requiredActivatorTag})", this);
        return ok;
    }

    private void RefreshGlobalTimeScale(bool useEnterDurationIfSlowing)
    {
        float desired = 1f;
        bool anyScaleFixed = false;

        foreach (var z in ActiveZones)
        {
            if (!z) continue;
            desired = Mathf.Min(desired, Mathf.Clamp01(z.targetTimeScaleWhileInside));
            anyScaleFixed |= z.scaleFixedDeltaTime;
        }

        float current = Time.timeScale;
        bool slowing = desired < current;
        float duration = slowing ? enterBlendDurationSeconds : exitBlendDurationSeconds;

        timeBlendHost = this;
        StartTimeBlend(desired, duration, anyScaleFixed);

        if (isDebugLoggingEnabled)
            Debug.Log($"[TimeDilationZone] {name}: TimeScale -> desired={desired:0.###}, current={current:0.###}, dur={duration:0.###}", this);
    }

    private static void StartTimeBlend(float target, float duration, bool scaleFixed)
    {
        if (timeBlendHost == null) return;
        if (timeBlendRoutine != null) timeBlendHost.StopCoroutine(timeBlendRoutine);
        timeBlendRoutine = timeBlendHost.StartCoroutine(BlendTimeScaleCoroutine(target, duration, scaleFixed));
    }

    private static IEnumerator BlendTimeScaleCoroutine(float target, float duration, bool scaleFixed)
    {
        target = Mathf.Clamp01(target);
        float start = Time.timeScale;

        if (Mathf.Approximately(start, target) || duration <= 0f)
        {
            Time.timeScale = target;
            if (scaleFixed)
                Time.fixedDeltaTime = defaultFixedDeltaTime * Mathf.Max(target, 0.0001f);
            else if (Mathf.Approximately(target, 1f))
                Time.fixedDeltaTime = defaultFixedDeltaTime;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            Time.timeScale = s;
            if (scaleFixed)
                Time.fixedDeltaTime = defaultFixedDeltaTime * Mathf.Max(s, 0.0001f);
            yield return null;
        }

        Time.timeScale = target;
        if (scaleFixed)
            Time.fixedDeltaTime = defaultFixedDeltaTime * Mathf.Max(target, 0.0001f);
        else if (Mathf.Approximately(target, 1f))
            Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    private void RefreshGlobalLowPass(bool useEnterDurationIfEntering)
    {
        if (!applyLowPassFilter) return;

        float desiredHz = globalOutsideCutoffHz;
        bool any = false;
        AudioMixer mixer = targetAudioMixer;
        string param = exposedLowPassParameterName;

        foreach (var z in ActiveZones)
        {
            if (!z) continue;
            if (!z.applyLowPassFilter) continue;
            if (!z.targetAudioMixer || string.IsNullOrEmpty(z.exposedLowPassParameterName)) continue;
            mixer = z.targetAudioMixer;
            param = z.exposedLowPassParameterName;
            desiredHz = Mathf.Min(desiredHz, Mathf.Max(10f, z.insideCutoffHz));
            any = true;
        }

        if (!any)
        {
            mixer = targetAudioMixer ? targetAudioMixer : mixer;
            param = string.IsNullOrEmpty(exposedLowPassParameterName) ? param : exposedLowPassParameterName;
            desiredHz = globalOutsideCutoffHz;
        }

        if (!mixer || string.IsNullOrEmpty(param)) return;

        audioBlendHost = this;
        activeAudioMixer = mixer;
        activeAudioParam = param;

        float currentHz;
        if (!activeAudioMixer.GetFloat(activeAudioParam, out currentHz)) currentHz = desiredHz;

        bool entering = desiredHz < currentHz;
        float duration = entering ? enterLowPassRampSeconds : exitLowPassRampSeconds;

        StartAudioBlend(desiredHz, duration);

        if (isDebugLoggingEnabled)
            Debug.Log($"[TimeDilationZone] {name}: LowPass -> desired={desiredHz:0.##} Hz, current={currentHz:0.##} Hz, dur={duration:0.###}", this);
    }

    private static void StartAudioBlend(float targetHz, float duration)
    {
        if (audioBlendHost == null) return;
        if (audioBlendRoutine != null) audioBlendHost.StopCoroutine(audioBlendRoutine);
        audioBlendRoutine = audioBlendHost.StartCoroutine(BlendAudioCoroutine(targetHz, duration));
    }

    private static IEnumerator BlendAudioCoroutine(float targetHz, float duration)
    {
        if (!activeAudioMixer || string.IsNullOrEmpty(activeAudioParam)) yield break;

        float startHz;
        if (!activeAudioMixer.GetFloat(activeAudioParam, out startHz)) startHz = targetHz;

        if (Mathf.Approximately(startHz, targetHz) || duration <= 0f)
        {
            activeAudioMixer.SetFloat(activeAudioParam, targetHz);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float v = Mathf.Lerp(startHz, targetHz, Mathf.SmoothStep(0f, 1f, t));
            activeAudioMixer.SetFloat(activeAudioParam, v);
            yield return null;
        }

        activeAudioMixer.SetFloat(activeAudioParam, targetHz);
    }

    private void RebuildOccupantsFromOverlap()
    {
        if (!zoneCollider) return;
        if (!zoneCollider.enabled || !zoneCollider.isTrigger) return;

        int hits = Physics.OverlapBoxNonAlloc(
            zoneCollider.bounds.center,
            zoneCollider.bounds.extents,
            TempBuffer, transform.rotation,
            ~0, QueryTriggerInteraction.Collide
        );

        int count = 0;
        for (int i = 0; i < hits; i++)
        {
            var col = TempBuffer[i];
            if (!col || col == zoneCollider) continue;
            if (IsActivator(col)) count++;
        }

        bool wasActive = currentQualifiedOccupantCount > 0;
        currentQualifiedOccupantCount = count;

        if (currentQualifiedOccupantCount > 0) ActiveZones.Add(this);
        else ActiveZones.Remove(this);

        if (wasActive != (currentQualifiedOccupantCount > 0))
        {
            if (ActiveZones.Count == 0) ForceResetEffectsImmediate(false);
            else
            {
                RefreshGlobalTimeScale(currentQualifiedOccupantCount > 0);
                RefreshGlobalLowPass(currentQualifiedOccupantCount > 0);
            }
        }
    }

    private static void ForceResetEffectsImmediate(bool clearStatics)
    {
        if (timeBlendRoutine != null && timeBlendHost) timeBlendHost.StopCoroutine(timeBlendRoutine);
        timeBlendRoutine = null;
        Time.timeScale = 1f;
        if (defaultFixedDeltaTime <= 0f) defaultFixedDeltaTime = Time.fixedDeltaTime;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        if (audioBlendRoutine != null && audioBlendHost) audioBlendHost.StopCoroutine(audioBlendRoutine);
        audioBlendRoutine = null;

        if (activeAudioMixer != null && !string.IsNullOrEmpty(activeAudioParam))
        {
            float hz = globalOutsideCutoffHz > 0f ? globalOutsideCutoffHz : 5000f;
            activeAudioMixer.SetFloat(activeAudioParam, hz);
        }

        if (clearStatics)
        {
            ActiveZones.Clear();
            timeBlendHost = null;
            audioBlendHost = null;
            activeAudioMixer = null;
            activeAudioParam = null;
        }
    }

    private static readonly Collider[] TempBuffer = new Collider[32];

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (!zoneCollider) zoneCollider = GetComponent<BoxCollider>();

        var m = transform.localToWorldMatrix;
        var size = zoneCollider ? zoneCollider.size : Vector3.one;
        var center = zoneCollider ? zoneCollider.center : Vector3.zero;

        Gizmos.matrix = m;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
