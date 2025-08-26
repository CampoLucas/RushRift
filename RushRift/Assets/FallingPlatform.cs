using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FallingPlatform : MonoBehaviour
{
    public enum FallMode
    {
        DynamicRigidbody,
        KinematicMovePosition
    }

    [Header("Activation")]
    [SerializeField, Tooltip("Objects with this tag can trigger the fall (usually 'Player'). Leave empty to accept any.")]
    private string requiredActivatorTag = "Player";

    [SerializeField, Tooltip("Also react to TRIGGER contacts (useful if your player uses a CharacterController). Add a trigger on this object or a child.")]
    private bool activateOnTriggerContacts = true;

    [Header("Timing")]
    [SerializeField, Tooltip("Delay in seconds before the platform starts falling after the first valid contact.")]
    private float fallDelaySeconds = 0.25f;

    [Header("Fall Mode")]
    [SerializeField, Tooltip("DynamicRigidbody: switch to non-kinematic and let physics/gravity take over.\nKinematicMovePosition: remain kinematic and simulate gravity via MovePosition (best with CharacterController).")]
    private FallMode fallMode = FallMode.KinematicMovePosition;

    [Header("Dynamic Rigidbody Settings")]
    [SerializeField, Tooltip("Enable gravity when the fall begins (DynamicRigidbody mode).")]
    private bool enableGravityOnFall = true;

    [SerializeField, Tooltip("Freeze platform rotations while falling (helps keep it level).")]
    private bool freezeRotationDuringFall = true;

    [SerializeField, Tooltip("Collision detection mode used when falling (helps prevent tunneling).")]
    private CollisionDetectionMode fallingCollisionDetection = CollisionDetectionMode.ContinuousSpeculative;

    [SerializeField, Tooltip("Interpolation used when falling (smoother visuals).")]
    private RigidbodyInterpolation fallingInterpolation = RigidbodyInterpolation.Interpolate;

    [Header("Kinematic Move Settings")]
    [SerializeField, Tooltip("Simulated gravity acceleration in m/s² for KinematicMovePosition mode.")]
    private float simulatedGravityAcceleration = 9.81f;

    [SerializeField, Tooltip("Maximum downward speed in m/s for KinematicMovePosition mode (0 = unlimited).")]
    private float simulatedMaxFallSpeed = 0f;

    [Header("Initial Kick")]
    [SerializeField, Tooltip("Extra downward velocity (m/s) applied at fall start (both modes).")]
    private float initialDownwardVelocity = 0f;

    [SerializeField, Tooltip("Downward impulse (N·s) applied at fall start (DynamicRigidbody mode only).")]
    private float initialDownwardImpulse = 0f;

    [Header("Debug")]
    [SerializeField, Tooltip("Enable to print debug logs to the Console.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw simple gizmos to visualize up/down.")]
    private bool drawGizmos = true;

    private Rigidbody platformRigidbody;
    private Collider platformCollider;

    private bool hasFallScheduled;
    private bool hasFallStarted;

    // Kinematic simulation state
    private float simulatedDownwardSpeed;

    private void Reset()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        platformCollider  = GetComponent<Collider>();

        platformRigidbody.isKinematic = true;   // start static
        platformRigidbody.useGravity  = false;  // no gravity initially
        platformRigidbody.interpolation = RigidbodyInterpolation.None;

        if (platformCollider) platformCollider.isTrigger = false; // solid by default

        Log("Reset → kinematic=TRUE, gravity=FALSE, collider.isTrigger=FALSE");
    }

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        platformCollider  = GetComponent<Collider>();

        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity  = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasFallStarted || hasFallScheduled) return;
        if (!IsActivator(collision)) return;
        ScheduleFallAfterDelay();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnTriggerContacts) return;
        if (hasFallStarted || hasFallScheduled) return;
        if (!IsActivator(other)) return;
        ScheduleFallAfterDelay();
    }

    private bool IsActivator(Collision collision)
    {
        var root = collision.rigidbody ? collision.rigidbody.gameObject : collision.transform.root.gameObject;
        bool ok = string.IsNullOrEmpty(requiredActivatorTag) || root.CompareTag(requiredActivatorTag);
        if (!ok) Log($"Collision ignored by tag: {root.name} (tag={root.tag}, required={requiredActivatorTag})");
        return ok;
    }

    private bool IsActivator(Collider other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        bool ok = string.IsNullOrEmpty(requiredActivatorTag) || root.CompareTag(requiredActivatorTag);
        if (!ok) Log($"Trigger ignored by tag: {root.name} (tag={root.tag}, required={requiredActivatorTag})");
        return ok;
    }

    private void ScheduleFallAfterDelay()
    {
        hasFallScheduled = true;
        Log($"Fall scheduled in {fallDelaySeconds:0.###}s.");
        if (fallDelaySeconds <= 0f) BeginFallNow();
        else StartCoroutine(FallAfterDelayCoroutine(fallDelaySeconds));
    }

    private IEnumerator FallAfterDelayCoroutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        BeginFallNow();
    }

    /// <summary>Begins the fall immediately (ignores delay).</summary>
    public void BeginFallNow()
    {
        if (hasFallStarted) return;
        hasFallStarted = true;

        if (fallMode == FallMode.DynamicRigidbody)
        {
            // Convert to a solid dynamic body
            platformCollider.isTrigger = false;
            platformRigidbody.isKinematic = false;
            platformRigidbody.useGravity = enableGravityOnFall;
            platformRigidbody.collisionDetectionMode = fallingCollisionDetection;
            platformRigidbody.interpolation = fallingInterpolation;
            platformRigidbody.constraints = freezeRotationDuringFall
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.None;
            platformRigidbody.WakeUp();

            if (initialDownwardImpulse != 0f)
                platformRigidbody.AddForce(-transform.up * initialDownwardImpulse, ForceMode.Impulse);

            if (initialDownwardVelocity != 0f)
                platformRigidbody.velocity += -transform.up * initialDownwardVelocity;

            Physics.SyncTransforms();
            Log($"Fall START (Dynamic) → kin={platformRigidbody.isKinematic}, grav={platformRigidbody.useGravity}, " +
                $"CD={platformRigidbody.collisionDetectionMode}, interp={platformRigidbody.interpolation}");
        }
        else // KinematicMovePosition
        {
            // Keep as kinematic and simulate gravity using MovePosition (best for CharacterController players)
            platformRigidbody.isKinematic = true;
            platformRigidbody.useGravity  = false;

            simulatedDownwardSpeed = Mathf.Max(0f, initialDownwardVelocity);
            Log($"Fall START (KinematicMovePosition) → startSpeed={simulatedDownwardSpeed:0.###} m/s");
        }
    }

    private void FixedUpdate()
    {
        if (!hasFallStarted) return;

        if (fallMode == FallMode.KinematicMovePosition)
        {
            // Integrate simple gravity and move the kinematic platform downwards
            simulatedDownwardSpeed += simulatedGravityAcceleration * Time.fixedDeltaTime;
            if (simulatedMaxFallSpeed > 0f)
                simulatedDownwardSpeed = Mathf.Min(simulatedDownwardSpeed, simulatedMaxFallSpeed);

            Vector3 delta = -transform.up * simulatedDownwardSpeed * Time.fixedDeltaTime;
            platformRigidbody.MovePosition(platformRigidbody.position + delta);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawRay(transform.position, transform.up * 0.4f);
        Gizmos.color = new Color(0.9f, 0.5f, 0.1f, 0.9f);
        Gizmos.DrawRay(transform.position, -transform.up * 0.4f);
    }
#endif

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FallingPlatform] {name}: {msg}", this);
    }
}
