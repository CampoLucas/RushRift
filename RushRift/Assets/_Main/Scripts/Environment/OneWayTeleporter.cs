using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.LevelElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class OneWayTeleporter : ObserverComponent
{
    public enum ExitAxisSource { DestinationUp, DestinationForward, Custom }

    [Header("Usage / Terminal Control")]
    [SerializeField, Tooltip("If disabled, this teleporter will not teleport anything. Toggled via OnNotify(ON/OFF).")]
    private bool canBeUsed = true;

    [Header("Destination")]
    [SerializeField, Tooltip("Where to place the object after teleporting.")]
    private Transform destination;
    [SerializeField, Tooltip("Local offset applied at the destination (use Z=+1 to appear in front).")]
    private Vector3 exitLocalOffset = new Vector3(0f, 0f, 1f);
    [SerializeField, Tooltip("If true, match the destination rotation on exit.")]
    private bool alignToDestinationRotation = true;

    [Header("Filtering")]
    [SerializeField, Tooltip("Only objects with this tag will be teleported. Leave empty to allow any.")]
    private string requiredTag = "Player";

    [Header("Velocity")]
    [SerializeField, Tooltip("Keep incoming velocity after teleport.")]
    private bool preserveVelocity = true;
    [SerializeField, Tooltip("If preserving velocity, remap source-space velocity into destination-space.")]
    private bool mapVelocityFromSourceToDestination = true;
    [SerializeField, Tooltip("Extra speed to add along destination.forward on exit.")]
    private float extraExitSpeed = 0f;

    [Header("Exit Velocity Control")]
    [SerializeField, Tooltip("Flips the velocity component along the exit axis (e.g., falling in becomes shooting upward).")]
    private bool flipVelocityAlongExitAxis = true;
    [SerializeField, Tooltip("Only flip if the object is moving against the axis (dot<0).")]
    private bool onlyFlipIfAgainstAxis = true;
    [SerializeField, Tooltip("Guarantee at least this speed along the positive exit axis after teleport.")]
    private float minExitSpeedAlongAxis = 0f;
    [SerializeField, Tooltip("Axis used for flipping / min speed checks.")]
    private ExitAxisSource exitAxis = ExitAxisSource.DestinationUp;
    [SerializeField, Tooltip("Used when ExitAxis is Custom. Will be normalized; if zero, falls back to world up.")]
    private Vector3 customExitAxis = Vector3.up;
    [SerializeField, Tooltip("Move the exit position a bit along the axis to avoid instant ground contact.")]
    private float exitClearanceAlongAxis = 0.15f;

    [Header("Safety")]
    [SerializeField, Tooltip("Minimum time between teleports for the same object.")]
    private float reTriggerCooldown = 0.2f;
    [SerializeField, Tooltip("Temporarily ignore collisions between the teleporter and the teleported object for one fixed update.")]
    private bool temporarilyIgnoreTeleporterColliders = true;

    [Header("Post-Teleport Enforcement")]
    [SerializeField, Tooltip("Re-apply the computed exit velocity for a few physics frames to fight other scripts overriding it.")]
    private bool reinforceExitVelocity = true;
    [SerializeField, Tooltip("How many FixedUpdate frames to re-apply the exit velocity for.")]
    private int reinforceFrames = 1;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed debug logs for this teleporter.")]
    private bool debugLogs = false;

    private readonly Dictionary<int, float> _lastTeleportTime = new();
    private readonly HashSet<Collider> _occupants = new();
    private static readonly Collider[] _overlapBuffer = new Collider[32];
    private Collider _collider;

    private void Awake() => _collider = GetComponent<Collider>();

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
        Log("Reset: set collider to trigger");
    }

    private void OnDisable() => _occupants.Clear();

    private void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        _occupants.Add(other);
        TryTeleport(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other) return;
        _occupants.Remove(other);
    }

    private void TryTeleport(Collider other)
    {
        if (!canBeUsed) { Log("Blocked: canBeUsed is false"); return; }
        if (!destination) { Log("No destination assigned"); return; }

        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if (!string.IsNullOrEmpty(requiredTag) && !root.CompareTag(requiredTag))
        {
            Log($"Ignored: {root.name} tag={root.tag}, required={requiredTag}");
            return;
        }

        int id = root.GetInstanceID();
        float now = Time.time;
        if (_lastTeleportTime.TryGetValue(id, out float last) && (now - last) < reTriggerCooldown)
        {
            Log($"Cooldown: {root.name} {(now - last):0.###}s < {reTriggerCooldown:0.###}s");
            return;
        }

        var rb = root.GetComponent<Rigidbody>();
        var axis = GetExitAxisNormalized();
        
        var baseExit = destination.TransformPoint(exitLocalOffset);
        var targetPos = baseExit + axis * Mathf.Max(0f, exitClearanceAlongAxis);
        var targetRot = alignToDestinationRotation ? destination.rotation : root.transform.rotation;

        if (rb)
        {
            var v = rb.velocity;

            if (preserveVelocity)
            {
                if (mapVelocityFromSourceToDestination)
                {
                    var local = transform.InverseTransformDirection(v);
                    v = destination.TransformDirection(local);
                }

                // Flip along axis (bounce effect)
                float dot = Vector3.Dot(v, axis);
                if (flipVelocityAlongExitAxis && (!onlyFlipIfAgainstAxis || dot < 0f))
                {
                    var vNew = v - 2f * dot * axis; // reflect component along axis
                    Log($"Flip axis={axis} dot={dot:0.###} | vel {v} -> {vNew}");
                    v = vNew;
                }

                // Ensure a minimum upward speed along axis
                if (minExitSpeedAlongAxis > 0f)
                {
                    float along = Vector3.Dot(v, axis);
                    if (along < minExitSpeedAlongAxis)
                    {
                        v += axis * (minExitSpeedAlongAxis - along);
                        Log($"Min speed along axis enforced: {along:0.###} -> {minExitSpeedAlongAxis:0.###}");
                    }
                }
            }
            else
            {
                v = Vector3.zero;
            }

            if (extraExitSpeed != 0f) v += destination.forward * extraExitSpeed;

            Log($"Teleport {root.name} -> pos={targetPos}, rot={(alignToDestinationRotation ? destination.rotation.eulerAngles : root.transform.rotation.eulerAngles)}, vel={v}");
            rb.position = targetPos;
            rb.rotation = targetRot;
            rb.velocity = v;

            if (temporarilyIgnoreTeleporterColliders) StartCoroutine(TempIgnore(root));
            if (reinforceExitVelocity && reinforceFrames > 0) StartCoroutine(ReinforceVelocity(rb, v, reinforceFrames));
        }
        else
        {
            Log($"Teleport {root.name} (no Rigidbody) -> pos={targetPos}");
            root.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        _lastTeleportTime[id] = now;
    }

    private Vector3 GetExitAxisNormalized()
    {
        Vector3 a = exitAxis switch
        {
            ExitAxisSource.DestinationUp => destination ? destination.up : Vector3.up,
            ExitAxisSource.DestinationForward => destination ? destination.forward : Vector3.forward,
            ExitAxisSource.Custom => customExitAxis,
            _ => Vector3.up
        };
        if (a.sqrMagnitude < 1e-6f) a = Vector3.up;
        return a.normalized;
    }

    private IEnumerator TempIgnore(GameObject root)
    {
        var myCols = GetComponents<Collider>();
        var otherCols = root.GetComponentsInChildren<Collider>();

        foreach (var a in myCols)
            foreach (var b in otherCols)
                if (a && b) Physics.IgnoreCollision(a, b, true);

        yield return new WaitForFixedUpdate();

        foreach (var a in myCols)
            foreach (var b in otherCols)
                if (a && b) Physics.IgnoreCollision(a, b, false);
    }

    private IEnumerator ReinforceVelocity(Rigidbody rb, Vector3 targetVel, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();
            if (!rb) yield break;
            rb.velocity = targetVel;
            Log($"Reinforce frame {i + 1}/{frames}: vel={rb.velocity}");
        }
    }

    public override void OnNotify(string arg)
    {
        if (arg == Terminal.ON_ARGUMENT)
        {
            canBeUsed = true;
            Log("OnNotify: ON -> checking occupants");
            RecheckAndTeleportOccupants();
        }
        else if (arg == Terminal.OFF_ARGUMENT)
        {
            canBeUsed = false;
            Log("OnNotify: OFF");
        }
        else
        {
            Log($"OnNotify: {arg} (ignored)");
        }
    }

    private void RecheckAndTeleportOccupants()
    {
        int teleported = 0;
        if (_occupants.Count > 0)
        {
            foreach (var col in _occupants)
            {
                if (!col) continue;
                TryTeleport(col);
                teleported++;
            }
        }
        
        else
        {
            int count = OverlapSelfNonAlloc(_overlapBuffer);
            
            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (!col || col == _collider) continue;
                TryTeleport(col);
                teleported++;
            }
        }
        Log($"Recheck complete: teleported {teleported} occupant(s).");
    }

    private int OverlapSelfNonAlloc(Collider[] buffer)
    {
        if (!_collider) return 0;

        if (_collider is BoxCollider box)
        {
            var center = box.transform.TransformPoint(box.center);
            var half = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            return Physics.OverlapBoxNonAlloc(center, half, buffer, box.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        }
        
        if (_collider is SphereCollider sphere)
        {
            var center = sphere.transform.TransformPoint(sphere.center);
            var radius = sphere.radius * MaxAbsComponent(sphere.transform.lossyScale);
            return Physics.OverlapSphereNonAlloc(center, radius, buffer, ~0, QueryTriggerInteraction.Collide);
        }
        
        if (_collider is CapsuleCollider capsule)
        {
            GetCapsuleWorld(capsule, out var p0, out var p1, out var r);
            return Physics.OverlapCapsuleNonAlloc(p0, p1, r, buffer, ~0, QueryTriggerInteraction.Collide);
        }

        var b = _collider.bounds;
        return Physics.OverlapBoxNonAlloc(b.center, b.extents, buffer, _collider.transform.rotation, ~0, QueryTriggerInteraction.Collide);
    }

    private static void GetCapsuleWorld(CapsuleCollider cc, out Vector3 p0, out Vector3 p1, out float radius)
    {
        var t = cc.transform;
        var lossy = t.lossyScale;
        float axisScale = cc.direction == 0 ? Mathf.Abs(lossy.x) : cc.direction == 1 ? Mathf.Abs(lossy.y) : Mathf.Abs(lossy.z);
        radius = cc.radius * MaxAbsComponent(lossy);
        float halfCyl = Mathf.Max(0f, (cc.height * 0.5f - cc.radius)) * axisScale;

        var center = t.TransformPoint(cc.center);
        Vector3 axis =
            cc.direction == 0 ? t.right :
            cc.direction == 1 ? t.up :
            t.forward;

        p0 = center + axis * halfCyl;
        p1 = center - axis * halfCyl;
    }

    private static float MaxAbsComponent(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void OnDrawGizmos()
    {
        if (!destination) return;
        var axis = GetExitAxisNormalized();

        Gizmos.color = canBeUsed ? Color.magenta : new Color(0.5f, 0.5f, 0.5f);
        var to = destination.TransformPoint(exitLocalOffset) + axis * Mathf.Max(0f, exitClearanceAlongAxis);
        Gizmos.DrawLine(transform.position, to);
        Gizmos.DrawWireSphere(to, 0.15f);
        Gizmos.DrawRay(to, destination.forward * 0.4f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(to, axis.normalized * 0.6f); // visualize flip/min-speed axis
    }

    private void Log(string msg)
    {
        if (!debugLogs) return;
        Debug.Log($"[OneWayTeleporter] {name}: {msg}", this);
    }
}
