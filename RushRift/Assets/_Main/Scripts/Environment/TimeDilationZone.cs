using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField, Tooltip("Global Time.timeScale while a qualifying object is inside. 1 = normal time, 0.25 = 25% speed.")]
    [Range(0.0f, 1.0f)]
    private float targetTimeScaleWhileInside = 0.3f;

    [SerializeField, Tooltip("Blend time (seconds) when entering the zone.")]
    private float enterBlendDurationSeconds = 0.10f;

    [SerializeField, Tooltip("Blend time (seconds) when exiting the zone (or when time speeds up).")]
    private float exitBlendDurationSeconds = 0.12f;

    [SerializeField, Tooltip("Scale Time.fixedDeltaTime along with Time.timeScale for consistent physics.")]
    private bool scaleFixedDeltaTime = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs and draws gizmos.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draws the zone bounds and current target in the scene view.")]
    private bool drawGizmos = true;

    private BoxCollider zoneCollider;
    private int currentQualifiedOccupantCount;
    
    private static readonly HashSet<TimeDilationZone> ActiveZones = new();
    private static Coroutine blendRoutine;
    private static MonoBehaviour blendHost;
    private static float defaultFixedDeltaTime = -1f;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
        if (defaultFixedDeltaTime < 0f) defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnValidate()
    {
        zoneCollider = GetComponent<BoxCollider>();
        if (autoConfigureColliderAsTrigger && zoneCollider) zoneCollider.isTrigger = true;
        
        targetTimeScaleWhileInside = Mathf.Clamp01(targetTimeScaleWhileInside);
        enterBlendDurationSeconds = Mathf.Max(0f, enterBlendDurationSeconds);
        exitBlendDurationSeconds = Mathf.Max(0f, exitBlendDurationSeconds);
    }

    private void OnEnable()
    {
        RebuildOccupantsFromOverlap();
    }

    private void OnDisable()
    {
        if (currentQualifiedOccupantCount > 0)
        {
            currentQualifiedOccupantCount = 0;
            ActiveZones.Remove(this);
            RefreshGlobalTimeScale(useEnterDurationIfSlowing: false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActivator(other)) return;

        currentQualifiedOccupantCount++;
        
        if (currentQualifiedOccupantCount == 1)
        {
            ActiveZones.Add(this);
            RefreshGlobalTimeScale(useEnterDurationIfSlowing: true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsActivator(other)) return;

        currentQualifiedOccupantCount = Mathf.Max(0, currentQualifiedOccupantCount - 1);
        
        if (currentQualifiedOccupantCount == 0)
        {
            ActiveZones.Remove(this);
            RefreshGlobalTimeScale(useEnterDurationIfSlowing: false);
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
        float desiredTimeScale = 1f;
        bool anyWantsFixedScaling = false;

        foreach (var zone in ActiveZones)
        {
            if (!zone) continue;
            desiredTimeScale = Mathf.Min(desiredTimeScale, Mathf.Clamp01(zone.targetTimeScaleWhileInside));
            anyWantsFixedScaling |= zone.scaleFixedDeltaTime;
        }

        desiredTimeScale = Mathf.Clamp(desiredTimeScale, 0f, 1f);
        
        float current = Time.timeScale;
        bool slowing = desiredTimeScale < current;
        float duration = slowing ? enterBlendDurationSeconds : exitBlendDurationSeconds;
        
        blendHost = this;
        StartBlend(desiredTimeScale, duration, anyWantsFixedScaling);

        if (isDebugLoggingEnabled)
            Debug.Log($"[TimeDilationZone] {name}: Refresh -> desired={desiredTimeScale:0.###}, current={current:0.###}, " +
                      $"blend={(slowing ? "enter" : "exit")} {duration:0.###}s, scaleFixed={anyWantsFixedScaling}", this);
    }

    private static void StartBlend(float target, float duration, bool scaleFixed)
    {
        if (blendHost == null) return;
        if (blendRoutine != null) blendHost.StopCoroutine(blendRoutine);
        blendRoutine = blendHost.StartCoroutine(BlendTimeScaleCoroutine(target, duration, scaleFixed));
    }

    private static IEnumerator BlendTimeScaleCoroutine(float target, float duration, bool scaleFixed)
    {
        target = Mathf.Clamp(target, 0f, 1f);
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
            RefreshGlobalTimeScale(useEnterDurationIfSlowing: currentQualifiedOccupantCount > 0);
    }

    private static readonly Collider[] TempBuffer = new Collider[32];

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (!zoneCollider) zoneCollider = GetComponent<BoxCollider>();

        var matrix = transform.localToWorldMatrix;
        var size = zoneCollider ? zoneCollider.size : Vector3.one;
        var center = zoneCollider ? zoneCollider.center : Vector3.zero;

        Gizmos.matrix = matrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(center, size);
        
        Vector3 labelPos = transform.TransformPoint(center + Vector3.up * (size.y * 0.5f + 0.05f));
        Vector3 arrowDir = Vector3.up * 0.25f;
        Gizmos.DrawRay(labelPos, arrowDir);
    }
#endif
}