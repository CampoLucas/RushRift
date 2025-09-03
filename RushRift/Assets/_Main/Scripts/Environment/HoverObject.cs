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

    [Header("Carry Player On Top")]
    [SerializeField, Tooltip("If enabled, a player standing on top is moved by the platform's vertical motion.")]
    private bool carryPlayerStandingOnTop = false;

    [SerializeField, Tooltip("Tag used to identify the player object standing on top.")]
    private string requiredPlayerTag = "Player";

    [SerializeField, Tooltip("Layers considered when checking for a player on top.")]
    private LayerMask playerTopCheckLayerMask = ~0;

    [SerializeField, Tooltip("Use the attached Collider bounds to define a thin overlap region above the platform.")]
    private bool useAttachedColliderForTopCheck = true;

    [SerializeField, Tooltip("Height of the overlap box used to detect the player on top, in meters.")]
    private float topCheckHeightMeters = 0.2f;

    [SerializeField, Tooltip("Inset applied to the overlap box in XZ from the platform bounds, in meters.")]
    private Vector2 topCheckInsetXZ = new Vector2(0.05f, 0.05f);

    [SerializeField, Tooltip("Prefer using CharacterController.Move for carrying when available.")]
    private bool preferCharacterControllerMove = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw gizmos for min/max hover positions and the top check region.")]
    private bool drawGizmos = true;

    private bool isHovering;
    private Vector3 baseWorldPosition;
    private Vector3 baseLocalPosition;
    private float phaseRadians;
    private float currentOffset;

    private Collider cachedCollider;
    private static readonly Collider[] overlapBuffer = new Collider[32];

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
    }

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
        if (!isHovering)
            return;

        float beforeY = transform.position.y;

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

        float afterY = transform.position.y;
        float deltaY = afterY - beforeY;

        if (carryPlayerStandingOnTop && Mathf.Abs(deltaY) > 0f)
            TryCarryPlayerByDeltaY(deltaY);
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

    private void TryCarryPlayerByDeltaY(float deltaY)
    {
        if (!useAttachedColliderForTopCheck && !cachedCollider)
            cachedCollider = GetComponent<Collider>();

        Bounds worldBounds;
        if (useAttachedColliderForTopCheck && cachedCollider)
            worldBounds = cachedCollider.bounds;
        else
        {
            var r = GetComponent<Renderer>();
            worldBounds = r ? r.bounds : new Bounds(transform.position, Vector3.one);
        }

        float insetX = Mathf.Max(0f, topCheckInsetXZ.x);
        float insetZ = Mathf.Max(0f, topCheckInsetXZ.y);
        float capY = Mathf.Max(0.01f, topCheckHeightMeters);

        Vector3 half = new Vector3(
            Mathf.Max(0.01f, worldBounds.extents.x - insetX),
            capY * 0.5f,
            Mathf.Max(0.01f, worldBounds.extents.z - insetZ)
        );

        Vector3 center = new Vector3(
            worldBounds.center.x,
            worldBounds.max.y + half.y,
            worldBounds.center.z
        );

        int hits = Physics.OverlapBoxNonAlloc(center, half, overlapBuffer, Quaternion.identity, playerTopCheckLayerMask, QueryTriggerInteraction.Collide);

        if (hits <= 0)
            return;

        Transform chosen = null;
        CharacterController chosenCC = null;
        Rigidbody chosenRB = null;

        for (int i = 0; i < hits; i++)
        {
            var c = overlapBuffer[i];
            if (!c) continue;
            var root = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform.root;
            if (!root) continue;
            if (!string.IsNullOrEmpty(requiredPlayerTag) && !root.CompareTag(requiredPlayerTag)) continue;
            chosen = root;
            chosenCC = root.GetComponent<CharacterController>();
            chosenRB = root.GetComponent<Rigidbody>();
            break;
        }

        if (!chosen)
            return;

        if (preferCharacterControllerMove && chosenCC && chosenCC.enabled)
        {
            chosenCC.Move(Vector3.up * deltaY);
            Log($"Carried CharacterController by {deltaY:0.###}m");
            return;
        }

        if (chosenRB)
        {
            if (chosenRB.isKinematic)
            {
                chosenRB.MovePosition(chosenRB.position + Vector3.up * deltaY);
                Log($"Carried kinematic Rigidbody by {deltaY:0.###}m");
            }
            else
            {
                var p = chosenRB.position;
                p.y += deltaY;
                chosenRB.position = p;
                Log($"Carried dynamic Rigidbody by {deltaY:0.###}m");
            }
            return;
        }

        var tp = chosen.position;
        tp.y += deltaY;
        chosen.position = tp;
        Log($"Carried Transform by {deltaY:0.###}m");
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

        Vector3 basePos = Application.isPlaying
            ? (useLocalSpace ? transform.parent ? transform.parent.TransformPoint(baseLocalPosition) : transform.position : baseWorldPosition)
            : transform.position;

        float amp = Mathf.Max(0f, hoverAmplitudeMeters);
        Vector3 minP = basePos + Vector3.down * amp;
        Vector3 maxP = basePos + Vector3.up * amp;

        Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.5f);
        Gizmos.DrawLine(minP, maxP);
        Gizmos.DrawSphere(minP, 0.025f);
        Gizmos.DrawSphere(maxP, 0.025f);

        if (!carryPlayerStandingOnTop) return;

        if (!cachedCollider) cachedCollider = GetComponent<Collider>();
        Bounds b;
        if (useAttachedColliderForTopCheck && cachedCollider) b = cachedCollider.bounds;
        else
        {
            var r = GetComponent<Renderer>();
            b = r ? r.bounds : new Bounds(transform.position, Vector3.one);
        }

        float insetX = Mathf.Max(0f, topCheckInsetXZ.x);
        float insetZ = Mathf.Max(0f, topCheckInsetXZ.y);
        float capY = Mathf.Max(0.01f, topCheckHeightMeters);

        Vector3 half = new Vector3(
            Mathf.Max(0.01f, b.extents.x - insetX),
            capY * 0.5f,
            Mathf.Max(0.01f, b.extents.z - insetZ)
        );

        Vector3 center = new Vector3(
            b.center.x,
            b.max.y + half.y,
            b.center.z
        );

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
        Gizmos.DrawCube(center, half * 2f);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireCube(center, half * 2f);
    }
#endif
}