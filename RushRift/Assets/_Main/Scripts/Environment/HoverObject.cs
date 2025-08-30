using UnityEngine;

[DisallowMultipleComponent]
public class HoverObject : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField, Tooltip("Peak vertical distance from the base position in meters.")]
    private float hoverAmplitudeMeters = 0.25f;

    [SerializeField, Tooltip("Oscillation speed in cycles per second.")]
    private float hoverSpeedCyclesPerSecond = 0.5f;

    [SerializeField, Tooltip("Run the hover using unscaled time.")]
    private bool useUnscaledTime = false;

    [SerializeField, Tooltip("Apply the hover along the local Y axis instead of world Y.")]
    private bool useLocalSpace = false;

    [SerializeField, Tooltip("Randomize the starting phase of the hover.")]
    private bool randomizeStartPhase = true;

    [SerializeField, Tooltip("Starting phase in degrees if randomize is disabled.")]
    private float startingPhaseDegrees = 0f;

    [Header("Lifecycle")]
    [SerializeField, Tooltip("Begin hovering automatically on OnEnable.")]
    private bool startHoveringOnEnable = true;

    [SerializeField, Tooltip("Reset the transform to its base position on OnDisable.")]
    private bool resetToBaseOnDisable = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw gizmos for min and max hover positions.")]
    private bool drawGizmos = true;

    private bool isHovering;
    private Vector3 baseWorldPosition;
    private Vector3 baseLocalPosition;
    private float phaseRadians;
    private float currentOffset;

    private void OnEnable()
    {
        CacheBasePosition();
        InitializePhase();
        if (startHoveringOnEnable) StartHover();
    }

    private void OnDisable()
    {
        StopHover();
        if (resetToBaseOnDisable) RestoreBasePosition();
    }

    private void Update()
    {
        if (!isHovering) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float twoPi = 6.28318530718f;
        phaseRadians += twoPi * Mathf.Max(0f, hoverSpeedCyclesPerSecond) * dt;
        if (phaseRadians > twoPi || phaseRadians < -twoPi) phaseRadians %= twoPi;

        currentOffset = Mathf.Sin(phaseRadians) * Mathf.Max(0f, hoverAmplitudeMeters);

        if (useLocalSpace)
        {
            var p = baseLocalPosition;
            p.y += currentOffset;
            transform.localPosition = p;
        }
        else
        {
            var p = baseWorldPosition;
            p.y += currentOffset;
            transform.position = p;
        }
    }

    public void StartHover()
    {
        isHovering = true;
        Log("Hover started");
    }

    public void StopHover()
    {
        isHovering = false;
        Log("Hover stopped");
    }

    public void SetHoverSpeed(float cyclesPerSecond)
    {
        hoverSpeedCyclesPerSecond = Mathf.Max(0f, cyclesPerSecond);
        Log($"Speed set to {hoverSpeedCyclesPerSecond:0.###} Hz");
    }

    public void SetHoverAmplitude(float meters)
    {
        hoverAmplitudeMeters = Mathf.Max(0f, meters);
        Log($"Amplitude set to {hoverAmplitudeMeters:0.###} m");
    }

    public void SetPhaseDegrees(float degrees)
    {
        phaseRadians = degrees * Mathf.Deg2Rad;
        Log($"Phase set to {degrees:0.##}°");
    }

    public void RebaseNow()
    {
        CacheBasePosition();
        Log("Rebased");
    }

    private void CacheBasePosition()
    {
        baseWorldPosition = transform.position;
        baseLocalPosition = transform.localPosition;
    }

    private void InitializePhase()
    {
        phaseRadians = randomizeStartPhase ? Random.value * 360f * Mathf.Deg2Rad : startingPhaseDegrees * Mathf.Deg2Rad;
    }

    private void RestoreBasePosition()
    {
        if (useLocalSpace) transform.localPosition = baseLocalPosition;
        else transform.position = baseWorldPosition;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[HoverObject] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 basePos = Application.isPlaying ? (useLocalSpace ? transform.parent ? transform.parent.TransformPoint(baseLocalPosition) : transform.position : baseWorldPosition) : transform.position;
        float amp = Mathf.Max(0f, hoverAmplitudeMeters);
        Vector3 minP = basePos + Vector3.down * amp;
        Vector3 maxP = basePos + Vector3.up * amp;

        Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.5f);
        Gizmos.DrawLine(minP, maxP);
        Gizmos.DrawSphere(minP, 0.025f);
        Gizmos.DrawSphere(maxP, 0.025f);
    }
#endif
}
