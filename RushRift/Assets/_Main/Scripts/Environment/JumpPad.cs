using Game.LevelElements.Terminal;
using UnityEngine;
using System.Collections.Generic;

namespace _Main.Scripts.Environment
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class JumpPad : ObserverComponent
    {
        private enum DirectionSource { WorldUp, PadUp, CustomVector, TransformForward }

        [Header("Usage / Terminal Control")]
        [SerializeField, Tooltip("If disabled, this pad will not launch the player. Toggled via OnNotify(ON/OFF).")]
        private bool canBeUsed = true;

        [Header("Launch")]
        [SerializeField, Tooltip("Target exit speed along the launch direction (m/s).")]
        private float launchSpeed = 12f;

        [Header("Direction")]
        [SerializeField, Tooltip("Where the launch direction comes from.")]
        private DirectionSource directionSource = DirectionSource.PadUp;

        [SerializeField, Tooltip("Used when Direction Source is CustomVector. Will be normalized.")]
        private Vector3 customDirection = Vector3.up;

        [SerializeField, Tooltip("Used when Direction Source is TransformForward. If null, world forward is used.")]
        private Transform directionTransform;

        [Header("Velocity Handling")]
        [SerializeField, Tooltip("If true, removes all sideways velocity relative to the launch direction.")]
        private bool resetTangentialVelocity = true;

        [SerializeField, Range(0f, 1f), Tooltip("Sideways velocity retention if not fully reset. 0 = none, 1 = keep all.")]
        private float tangentialRetention01;

        [SerializeField, Tooltip("If true, the final velocity magnitude will be exactly Launch Speed, regardless of entry angle.")]
        private bool clampExitSpeedToLaunch = true;

        [Header("Triggering")]
        [SerializeField, Tooltip("If true, launch once on entry. If false, launch continuously while inside the trigger.")]
        private bool triggerOnlyOnEnter = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed debug logs for this JumpPad.")]
        private bool debugLogs;

        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private readonly HashSet<Collider> _occupants = new();
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
            Log("Reset: set collider to trigger");
        }

        private void OnDisable()
        {
            _occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other) return;
            _occupants.Add(other);
            if (triggerOnlyOnEnter) TryLaunch(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other) return;
            _occupants.Remove(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!triggerOnlyOnEnter) TryLaunch(other);
        }

        private void TryLaunch(Collider other)
        {
            if (!canBeUsed)
            {
                Log("Blocked: canBeUsed is false");
                return;
            }

            var rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
            if (!rb)
            {
                Log($"Ignored: no Rigidbody on {other.name}");
                return;
            }

            var go = rb.gameObject;
            if (!go.CompareTag("Player"))
            {
                Log($"Ignored: {go.name} does not have Player tag (tag={go.tag})");
                return;
            }

            Vector3 dir = GetDirection();
            Vector3 v = rb.velocity;

            float along = Vector3.Dot(v, dir);
            Vector3 tangential = v - dir * along;

            if (resetTangentialVelocity) tangential = Vector3.zero;
            else tangential *= Mathf.Clamp01(tangentialRetention01);

            Vector3 newVelocity;

            if (clampExitSpeedToLaunch)
            {
                newVelocity = dir * Mathf.Max(0f, launchSpeed);
            }
            else
            {
                newVelocity = tangential + dir * Mathf.Max(0f, launchSpeed);
            }

            Log($"Launch {go.name} | entrySpeed={v.magnitude:0.###} | tangentialKept={tangential.magnitude:0.###} | exitSpeed={newVelocity.magnitude:0.###} | dir={dir}");
            rb.velocity = newVelocity;
        }

        private Vector3 GetDirection()
        {
            Vector3 direction = directionSource switch
            {
                DirectionSource.WorldUp         => Vector3.up,
                DirectionSource.PadUp           => transform.up,
                DirectionSource.CustomVector    => customDirection,
                DirectionSource.TransformForward=> directionTransform ? directionTransform.forward : Vector3.forward,
                _ => transform.up
            };
            if (direction.sqrMagnitude < 1e-6f) direction = Vector3.up;
            return direction.normalized;
        }

        public override void OnNotify(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return;

            string a = arg.Trim().ToLowerInvariant();
            if (a == Terminal.ON_ARGUMENT)
            {
                canBeUsed = true;
                Log("OnNotify: ON -> rechecking occupants");
                RecheckAndLaunchOccupants();
            }
            else if (a == Terminal.OFF_ARGUMENT)
            {
                canBeUsed = false;
                Log("OnNotify: OFF");
            }
            else
            {
                Log($"OnNotify: {arg} (ignored)");
            }
        }

        private void RecheckAndLaunchOccupants()
        {
            int launched = 0;

            if (_occupants.Count > 0)
            {
                foreach (var col in _occupants)
                {
                    if (!col) continue;
                    TryLaunch(col);
                    launched++;
                }
            }
            else
            {
                int count = OverlapSelfNonAlloc(OverlapBuffer);
                for (int i = 0; i < count; i++)
                {
                    var col = OverlapBuffer[i];
                    if (!col || col == _collider) continue;
                    TryLaunch(col);
                    launched++;
                }
            }

            Log($"Recheck complete: launched {launched} occupant(s).");
        }

        private int OverlapSelfNonAlloc(Collider[] buffer)
        {
            if (!_collider) return 0;

            if (_collider is BoxCollider box)
            {
                Vector3 center = box.transform.TransformPoint(box.center);
                Vector3 half = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
                return Physics.OverlapBoxNonAlloc(center, half, buffer, box.transform.rotation, ~0, QueryTriggerInteraction.Collide);
            }

            if (_collider is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * MaxAbsComponent(sphere.transform.lossyScale);
                return Physics.OverlapSphereNonAlloc(center, radius, buffer, ~0, QueryTriggerInteraction.Collide);
            }

            if (_collider is CapsuleCollider capsule)
            {
                GetCapsuleWorld(capsule, out Vector3 p0, out Vector3 p1, out float r);
                return Physics.OverlapCapsuleNonAlloc(p0, p1, r, buffer, ~0, QueryTriggerInteraction.Collide);
            }

            Bounds b = _collider.bounds;
            return Physics.OverlapBoxNonAlloc(b.center, b.extents, buffer, _collider.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        }

        private static void GetCapsuleWorld(CapsuleCollider c, out Vector3 p0, out Vector3 p1, out float radius)
        {
            Transform t = c.transform;
            Vector3 s = t.lossyScale;

            float axisScale = c.direction == 0 ? Mathf.Abs(s.x) : c.direction == 1 ? Mathf.Abs(s.y) : Mathf.Abs(s.z);
            radius = c.radius * MaxAbsComponent(s);
            float halfCyl = Mathf.Max(0f, (c.height * 0.5f - c.radius)) * axisScale;

            Vector3 center = t.TransformPoint(c.center);
            Vector3 axis = c.direction == 0 ? t.right : c.direction == 1 ? t.up : t.forward;

            p0 = center + axis * halfCyl;
            p1 = center - axis * halfCyl;
        }

        private static float MaxAbsComponent(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private void OnDrawGizmosSelected()
        {
            Vector3 dir = GetDirection();
            Gizmos.color = canBeUsed ? Color.cyan : new Color(0.5f, 0.5f, 0.5f);
            Gizmos.DrawRay(transform.position, dir * Mathf.Max(launchSpeed * 0.25f, 0.25f));
        }

        private void Log(string msg)
        {
            if (!debugLogs) return;
            Debug.Log($"[JumpPad] {name}: {msg}", this);
        }
    }
}