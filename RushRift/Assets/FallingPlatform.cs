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

    public enum ActivationMethod
    {
        CollisionOrTriggerMessages,
        OverlapBoxCheck
    }

    [Header("Activation (General)")]
    [SerializeField, Tooltip("Objects with this tag can trigger the fall (usually 'Player'). Leave empty to accept any.")]
    private string requiredActivatorTag = "Player";

    [SerializeField, Tooltip("Choose how the platform detects the player contact. OverlapBoxCheck is the most reliable with CharacterController.")]
    private ActivationMethod activationMethod = ActivationMethod.OverlapBoxCheck;

    [SerializeField, Tooltip("Also react to TRIGGER contacts (useful if your player uses a CharacterController). Requires a trigger on this object or a child.")]
    private bool activateOnTriggerContacts = true;

    [Header("Activation (Overlap Check)")]
    [SerializeField, Tooltip("Local-space center for the OverlapBox that detects the player.")]
    private Vector3 activationOverlapCenterLocal = new Vector3(0f, 0.35f, 0f);

    [SerializeField, Tooltip("Half extents (X,Y,Z) of the OverlapBox. Make it cover the top area of the platform.")]
    private Vector3 activationOverlapHalfExtents = new Vector3(0.7f, 0.5f, 0.7f);

    [SerializeField, Tooltip("Layers considered by the OverlapBox. Leave to Everything and filter by tag, or restrict to your Player layer.")]
    private LayerMask activationOverlapLayerMask = ~0;

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

    [SerializeField, Tooltip("Draw gizmos for the activation overlap and platform up/down.")]
    private bool drawGizmos = true;

    private Rigidbody platformRigidbody;
    private Collider platformCollider;

    private bool hasFallScheduled;
    private bool hasFallStarted;
    
    private float simulatedDownwardSpeed;
    
    private static readonly Collider[] overlapBuffer = new Collider[32];

    private void Reset()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        platformCollider  = GetComponent<Collider>();
        
        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity  = false;
        platformRigidbody.interpolation = RigidbodyInterpolation.None;

        if (platformCollider) platformCollider.isTrigger = false; // solid "Ground" collider

        Log("Reset → kinematic=TRUE, gravity=FALSE, collider.isTrigger=FALSE");
    }

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        platformCollider  = GetComponent<Collider>();
        
        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity  = false;
    }

    private void FixedUpdate()
    {
        if (hasFallStarted || hasFallScheduled) return;

        if (activationMethod == ActivationMethod.OverlapBoxCheck)
        {
            if (CheckActivationByOverlap())
            {
                ScheduleFallAfterDelay();
            }
        }
        
        if (hasFallStarted && fallMode == FallMode.KinematicMovePosition)
        {
            simulatedDownwardSpeed += simulatedGravityAcceleration * Time.fixedDeltaTime;
            if (simulatedMaxFallSpeed > 0f)
                simulatedDownwardSpeed = Mathf.Min(simulatedDownwardSpeed, simulatedMaxFallSpeed);

            Vector3 delta = -transform.up * simulatedDownwardSpeed * Time.fixedDeltaTime;
            platformRigidbody.MovePosition(platformRigidbody.position + delta);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (activationMethod != ActivationMethod.CollisionOrTriggerMessages) return;
        
        if (hasFallStarted || hasFallScheduled) return;
        
        if (!IsActivator(collision)) return;
        ScheduleFallAfterDelay();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activationMethod != ActivationMethod.CollisionOrTriggerMessages) return;
        if (!activateOnTriggerContacts) return;
        if (hasFallStarted || hasFallScheduled) return;
        if (!IsActivator(other)) return;
        ScheduleFallAfterDelay();
    }
    
    private bool CheckActivationByOverlap()
    {
        var centerWorld = transform.TransformPoint(activationOverlapCenterLocal);
        var count = Physics.OverlapBoxNonAlloc(
            centerWorld,
            activationOverlapHalfExtents,
            overlapBuffer,
            transform.rotation,
            activationOverlapLayerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (!col || col == platformCollider) continue;

            var root = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.transform.root.gameObject;
            
            if (string.IsNullOrEmpty(requiredActivatorTag) || root.CompareTag(requiredActivatorTag))
            {
                Log($"Activation by Overlap → {root.name}");
                return true;
            }
        }
        
        return false;
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
    
    public void BeginFallNow()
    {
        if (hasFallStarted) return;
        hasFallStarted = true;

        if (fallMode == FallMode.DynamicRigidbody)
        {
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
            Log($"Fall START (Dynamic) → kin={platformRigidbody.isKinematic}, grav={platformRigidbody.useGravity}");
        }
        
        else
        {
            platformRigidbody.isKinematic = true;
            platformRigidbody.useGravity  = false;

            simulatedDownwardSpeed = Mathf.Max(0f, initialDownwardVelocity);
            Log($"Fall START (KinematicMovePosition) → startSpeed={simulatedDownwardSpeed:0.###} m/s");
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
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        var centerWorld = transform.TransformPoint(activationOverlapCenterLocal);
        var r = transform.rotation;
        
        Matrix4x4 matrix = Matrix4x4.TRS(centerWorld, r, Vector3.one * 2f);
        
        Gizmos.matrix = matrix;
        Gizmos.DrawCube(Vector3.zero, activationOverlapHalfExtents);
        Gizmos.color = new Color(1f, 1f, 0f, 0.85f);
        Gizmos.DrawWireCube(Vector3.zero, activationOverlapHalfExtents);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FallingPlatform] {name}: {msg}", this);
    }
}
