using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TractorBeam : ObserverComponent
{
    public enum DirectionSource { BeamForward, WorldDirection, CustomVector, TransformForward }

    [Header("Area")]
    [SerializeField, Tooltip("Collider that defines the tractor beam volume.")]
    private Collider areaCollider;
    [SerializeField, Tooltip("Layers considered as occupants inside the beam.")]
    private LayerMask occupantLayerMask = ~0;
    [SerializeField, Tooltip("Include trigger colliders in overlap checks.")]
    private bool includeTriggerColliders = true;
    [SerializeField, Tooltip("Seconds to tolerate a missed overlap before treating an object as exited.")]
    private float exitGraceSeconds = 0.1f;

    [Header("Usage / Control")]
    [SerializeField, Tooltip("If false, the beam is disabled.")]
    private bool canBeUsed = true;
    [SerializeField, Tooltip("Argument that enables the beam in OnNotify.")]
    private string onArgument = "ON";
    [SerializeField, Tooltip("Argument that disables the beam in OnNotify.")]
    private string offArgument = "OFF";
    [SerializeField, Tooltip("Only objects with this tag are affected. Leave empty to affect any.")]
    private string requiredActivatorTag = "Player";

    [Header("Direction")]
    [SerializeField, Tooltip("Where the beam transports objects.")]
    private DirectionSource directionSource = DirectionSource.BeamForward;
    [SerializeField, Tooltip("World-space direction when using WorldDirection.")]
    private Vector3 worldDirection = Vector3.up;
    [SerializeField, Tooltip("Direction used when CustomVector is selected.")]
    private Vector3 customDirection = Vector3.up;
    [SerializeField, Tooltip("Reference when using TransformForward.")]
    private Transform directionTransform;

    [Header("Transport")]
    [SerializeField, Tooltip("Acceleration along the beam direction (m/s²).")]
    private float beamAcceleration = 12f;
    [SerializeField, Tooltip("Maximum speed along the beam direction (m/s).")]
    private float beamMaxSpeed = 4f;
    [SerializeField, Tooltip("Damping per second applied to sideways velocity.")]
    private float tangentialDamping = 8f;
    [SerializeField, Tooltip("Clamp sideways speed to this maximum (0 = unlimited).")]
    private float maxTangentialSpeed = 1.5f;
    [SerializeField, Tooltip("Clamp total speed inside to this maximum (0 = unlimited).")]
    private float maxTotalSpeedInside = 5f;

    [Header("Float Stabilization")]
    [SerializeField, Tooltip("Gradually remove vertical (world Y) velocity to keep floating.")]
    private bool stabilizeVerticalVelocity = true;
    [SerializeField, Tooltip("Rate per second at which vertical velocity is driven toward zero.")]
    private float verticalStabilizationRate = 10f;

    [Header("Zero Gravity")]
    [SerializeField, Tooltip("Disable Rigidbody gravity while inside.")]
    private bool disableGravityWhileInside = true;
    [SerializeField, Tooltip("Override Rigidbody drag while inside.")]
    private bool useOverrideDrag = true;
    [SerializeField, Tooltip("Drag value applied while inside if override is enabled.")]
    private float overrideDrag = 4f;
    [SerializeField, Tooltip("Movement multiplier communicated to ZeroGravityReceiver while inside.")]
    private float zeroGMoveMultiplier = 0.25f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw gizmos for direction and area.")]
    private bool drawGizmos = true;

    private readonly HashSet<Rigidbody> currentBodies = new();
    private readonly Dictionary<Rigidbody, SavedBodyState> savedStates = new();
    private readonly Dictionary<Rigidbody, float> lastSeenTime = new();
    private static readonly Collider[] overlapBuffer = new Collider[64];

    private struct SavedBodyState { public bool useGravity; public float drag; }

    private void Awake()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider>();
    }

    private void OnValidate()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider>();
        beamAcceleration = Mathf.Max(0f, beamAcceleration);
        beamMaxSpeed = Mathf.Max(0f, beamMaxSpeed);
        tangentialDamping = Mathf.Max(0f, tangentialDamping);
        maxTangentialSpeed = Mathf.Max(0f, maxTangentialSpeed);
        maxTotalSpeedInside = Mathf.Max(0f, maxTotalSpeedInside);
        exitGraceSeconds = Mathf.Max(0f, exitGraceSeconds);
        verticalStabilizationRate = Mathf.Max(0f, verticalStabilizationRate);
        overrideDrag = Mathf.Max(0f, overrideDrag);
        zeroGMoveMultiplier = Mathf.Clamp(zeroGMoveMultiplier, 0.05f, 1f);
    }

    private void OnDisable()
    {
        foreach (var rb in currentBodies)
        {
            var root = rb ? rb.gameObject : null;
            if (root)
            {
                var recv = root.GetComponent<ZeroGravityReceiver>();
                if (recv) recv.ExitZeroG(this);
            }
        }
        RestoreAllBodies();
        currentBodies.Clear();
        savedStates.Clear();
        lastSeenTime.Clear();
    }

    private void FixedUpdate()
    {
        if (!areaCollider) return;

        int hits = OverlapAreaNonAlloc(overlapBuffer);
        float now = Time.time;

        for (int i = 0; i < hits; i++)
        {
            var col = overlapBuffer[i];
            if (!col || col == areaCollider) continue;

            var rb = col.attachedRigidbody ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
            if (!rb) continue;

            var root = rb.gameObject;
            if (!string.IsNullOrEmpty(requiredActivatorTag) && !root.CompareTag(requiredActivatorTag)) continue;

            lastSeenTime[rb] = now;

            if (currentBodies.Add(rb))
            {
                if (!savedStates.ContainsKey(rb))
                    savedStates[rb] = new SavedBodyState { useGravity = rb.useGravity, drag = rb.drag };
                if (disableGravityWhileInside) rb.useGravity = false;
                if (useOverrideDrag) rb.drag = overrideDrag;

                var recv = root.GetComponent<ZeroGravityReceiver>();
                if (recv) recv.EnterZeroG(this, zeroGMoveMultiplier);

                Log($"Enter: {root.name}");
            }
        }

        if (currentBodies.Count > 0)
        {
            var toRemove = ListPool<Rigidbody>.Get();
            foreach (var rb in currentBodies)
            {
                if (!rb) { toRemove.Add(rb); continue; }
                if (!lastSeenTime.TryGetValue(rb, out var ts)) { toRemove.Add(rb); continue; }
                if (now - ts > exitGraceSeconds) toRemove.Add(rb);
            }
            foreach (var rb in toRemove)
            {
                var root = rb ? rb.gameObject : null;
                if (currentBodies.Remove(rb))
                {
                    if (root)
                    {
                        var recv = root.GetComponent<ZeroGravityReceiver>();
                        if (recv) recv.ExitZeroG(this);
                    }
                    RestoreBody(rb);
                    lastSeenTime.Remove(rb);
                    Log($"Exit: {(root ? root.name : "null")}");
                }
            }
            ListPool<Rigidbody>.Release(toRemove);
        }

        if (!canBeUsed || currentBodies.Count == 0) return;

        var dir = GetDirectionNormalized();
        float dt = Time.fixedDeltaTime;
        float tangentialDecay = Mathf.Exp(-tangentialDamping * dt);

        foreach (var rb in currentBodies)
        {
            if (!rb) continue;

            var v = rb.velocity;

            if (stabilizeVerticalVelocity)
            {
                float vy = v.y;
                float vyTarget = Mathf.MoveTowards(vy, 0f, verticalStabilizationRate * dt);
                v += Vector3.up * (vyTarget - vy);
            }

            float along = Vector3.Dot(v, dir);
            var tangential = v - dir * along;
            tangential *= tangentialDecay;

            if (maxTangentialSpeed > 0f)
            {
                float mag = tangential.magnitude;
                if (mag > maxTangentialSpeed) tangential = tangential * (maxTangentialSpeed / mag);
            }

            float targetAlong = Mathf.MoveTowards(along, beamMaxSpeed, beamAcceleration * dt);
            var newVel = tangential + dir * targetAlong;

            if (maxTotalSpeedInside > 0f)
            {
                float m = newVel.magnitude;
                if (m > maxTotalSpeedInside) newVel = newVel * (maxTotalSpeedInside / m);
            }

            rb.velocity = newVel;
        }
    }

    public override void OnNotify(string arg)
    {
        string a = (arg ?? "").Trim().ToUpperInvariant();
        bool turnOn = a == onArgument.ToUpperInvariant() || a == "ON" || a == "ENABLE";
        bool turnOff = a == offArgument.ToUpperInvariant() || a == "OFF" || a == "DISABLE";

        if (turnOn)
        {
            canBeUsed = true;
            Log("On");
            ReapplyInsideBodies();
        }
        else if (turnOff)
        {
            canBeUsed = false;
            Log("Off");
            foreach (var rb in currentBodies)
            {
                var root = rb ? rb.gameObject : null;
                if (root)
                {
                    var recv = root.GetComponent<ZeroGravityReceiver>();
                    if (recv) recv.ExitZeroG(this);
                }
            }
            RestoreAllBodies();
            currentBodies.Clear();
            savedStates.Clear();
            lastSeenTime.Clear();
        }
        else
        {
            Log($"OnNotify ignored: {arg}");
        }
    }

    private void ReapplyInsideBodies()
    {
        if (!areaCollider) return;
        int hits = OverlapAreaNonAlloc(overlapBuffer);
        float now = Time.time;

        for (int i = 0; i < hits; i++)
        {
            var col = overlapBuffer[i];
            if (!col || col == areaCollider) continue;

            var rb = col.attachedRigidbody ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
            if (!rb) continue;

            var root = rb.gameObject;
            if (!string.IsNullOrEmpty(requiredActivatorTag) && !root.CompareTag(requiredActivatorTag)) continue;

            lastSeenTime[rb] = now;

            if (currentBodies.Add(rb))
            {
                if (!savedStates.ContainsKey(rb))
                    savedStates[rb] = new SavedBodyState { useGravity = rb.useGravity, drag = rb.drag };
                if (disableGravityWhileInside) rb.useGravity = false;
                if (useOverrideDrag) rb.drag = overrideDrag;

                var recv = root.GetComponent<ZeroGravityReceiver>();
                if (recv) recv.EnterZeroG(this, zeroGMoveMultiplier);

                Log($"Applied to occupant: {root.name}");
            }
        }
    }

    private int OverlapAreaNonAlloc(Collider[] buffer)
    {
        var qti = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (areaCollider is BoxCollider box)
        {
            var center = box.transform.TransformPoint(box.center);
            var half = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            return Physics.OverlapBoxNonAlloc(center, half, buffer, box.transform.rotation, occupantLayerMask, qti);
        }
        if (areaCollider is SphereCollider sphere)
        {
            var center = sphere.transform.TransformPoint(sphere.center);
            var radius = sphere.radius * MaxAbsComponent(sphere.transform.lossyScale);
            return Physics.OverlapSphereNonAlloc(center, radius, buffer, occupantLayerMask, qti);
        }
        if (areaCollider is CapsuleCollider capsule)
        {
            GetCapsuleWorld(capsule, out var p0, out var p1, out var r);
            return Physics.OverlapCapsuleNonAlloc(p0, p1, r, buffer, occupantLayerMask, qti);
        }

        var b = areaCollider.bounds;
        return Physics.OverlapBoxNonAlloc(b.center, b.extents, buffer, areaCollider.transform.rotation, occupantLayerMask, qti);
    }

    private void RestoreAllBodies()
    {
        foreach (var rb in currentBodies) RestoreBody(rb);
    }

    private void RestoreBody(Rigidbody rb)
    {
        if (!rb) return;
        if (savedStates.TryGetValue(rb, out var s))
        {
            rb.useGravity = s.useGravity;
            if (useOverrideDrag) rb.drag = s.drag;
        }
    }

    private Vector3 GetDirectionNormalized()
    {
        Vector3 d;
        switch (directionSource)
        {
            case DirectionSource.WorldDirection: d = worldDirection; break;
            case DirectionSource.CustomVector: d = customDirection; break;
            case DirectionSource.TransformForward: d = directionTransform ? directionTransform.forward : transform.forward; break;
            default: d = transform.forward; break;
        }
        if (d.sqrMagnitude < 1e-6f) d = Vector3.forward;
        return d.normalized;
    }

    private static void GetCapsuleWorld(CapsuleCollider cc, out Vector3 p0, out Vector3 p1, out float radius)
    {
        var t = cc.transform;
        var s = t.lossyScale;
        float axisScale = cc.direction == 0 ? Mathf.Abs(s.x) : cc.direction == 1 ? Mathf.Abs(s.y) : Mathf.Abs(s.z);
        radius = cc.radius * MaxAbsComponent(s);
        float halfCyl = Mathf.Max(0f, (cc.height * 0.5f - cc.radius)) * axisScale;
        var c = t.TransformPoint(cc.center);
        Vector3 axis = cc.direction == 0 ? t.right : cc.direction == 1 ? t.up : t.forward;
        p0 = c + axis * halfCyl;
        p1 = c - axis * halfCyl;
    }

    private static float MaxAbsComponent(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        var dir = GetDirectionNormalized();
        Gizmos.color = canBeUsed ? Color.cyan : new Color(0.5f, 0.5f, 0.5f);
        Gizmos.DrawRay(transform.position, dir * 0.8f);

        if (areaCollider)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            if (areaCollider is BoxCollider box)
            {
                var m = Matrix4x4.TRS(box.transform.TransformPoint(box.center), box.transform.rotation, Vector3.Scale(box.size, box.transform.lossyScale));
                Gizmos.matrix = m;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                var b = areaCollider.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }
#endif

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[TractorBeam] {name}: {msg}", this);
    }

    private static class ListPool<T>
    {
        static readonly Stack<List<T>> pool = new();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(16);
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}