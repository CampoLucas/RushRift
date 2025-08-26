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
        [SerializeField, Tooltip("Launch speed along the chosen direction (m/s).")]
        private float launchSpeed = 12f;

        [Header("Direction")]
        [SerializeField, Tooltip("Where the launch direction comes from.")]
        private DirectionSource directionSource = DirectionSource.PadUp;

        [SerializeField, Tooltip("Used when Direction Source is CustomVector. Will be normalized.")]
        private Vector3 customDirection = Vector3.up;

        [SerializeField, Tooltip("Used when Direction Source is TransformForward. If null, world forward is used.")]
        private Transform directionTransform;

        [Header("Velocity Handling")]
        [SerializeField, Tooltip("If true, zeroes sideways velocity relative to the launch direction.")]
        private bool resetTangentialVelocity = false;

        [Header("Triggering")]
        [SerializeField, Tooltip("If true, launch once on entry. If false, launch continuously while inside the trigger.")]
        private bool triggerOnlyOnEnter = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed debug logs for this JumpPad.")]
        private bool debugLogs;

        private static readonly Collider[] _overlapBuffer = new Collider[32];
        private readonly HashSet<Collider> _occupants = new();
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Reset()
        {
            Collider jumpPadCollider = GetComponent<Collider>();
            if (jumpPadCollider) jumpPadCollider.isTrigger = true;
            
            Log("Reset: set collider to trigger");
        }

        private void OnDisable()
        {
            _occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            _occupants.Add(other);
            
            if (triggerOnlyOnEnter) TryLaunch(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
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

            Rigidbody targetRigidbody = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
            
            if (!targetRigidbody)
            {
                Log($"Ignored: no Rigidbody on {other.name}");
                return;
            }

            GameObject go = targetRigidbody.gameObject;
            if (!go.CompareTag("Player"))
            {
                Log($"Ignored: {go.name} does not have Player tag (tag={go.tag})");
                return;
            }

            Vector3 dir = GetDirection();
            Vector3 velocity = targetRigidbody.velocity;

            float along = Vector3.Dot(velocity, dir);
            Vector3 tangential = velocity - dir * along;
            if (resetTangentialVelocity) tangential = Vector3.zero;

            Vector3 newVelocity = tangential + dir * Mathf.Max(launchSpeed, 0f);
            Log($"Launching {go.name}: dir={dir}, speed={launchSpeed:0.##}, oldVel={velocity}, newVel={newVelocity}");
            targetRigidbody.velocity = newVelocity;
        }

        private Vector3 GetDirection()
        {
            Vector3 direction = directionSource switch
            {
                DirectionSource.WorldUp => Vector3.up,
                DirectionSource.PadUp => transform.up,
                DirectionSource.CustomVector => customDirection,
                DirectionSource.TransformForward => directionTransform ? directionTransform.forward : Vector3.forward,
                _ => transform.up
            };

            if (direction.sqrMagnitude < 1e-6f) direction = Vector3.up;
            return direction.normalized;
        }

        public override void OnNotify(string arg)
        {
            if (arg == Terminal.ON_ARGUMENT)
            {
                canBeUsed = true;
                Log("OnNotify: ON -> checking occupants");
                RecheckAndLaunchOccupants();
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

        private void RecheckAndLaunchOccupants()
        {
            int launched = 0;
            
            if (_occupants.Count > 0)
            {
                foreach (Collider col in _occupants)
                {
                    if (!col) continue;
                    TryLaunch(col);
                    launched++;
                }
            }
            
            else
            {
                int count = OverlapSelfNonAlloc(_overlapBuffer);
                
                for (int i = 0; i < count; i++)
                {
                    Collider col = _overlapBuffer[i];
                    
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
            
            Bounds colliderBounds = _collider.bounds;
            return Physics.OverlapBoxNonAlloc(colliderBounds.center, colliderBounds.extents, buffer, _collider.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        }

        private static void GetCapsuleWorld(CapsuleCollider capsuleCollider, out Vector3 p0, out Vector3 p1, out float radius)
        {
            Transform colliderTransform = capsuleCollider.transform;
            Vector3 lossy = colliderTransform.lossyScale;
            
            float axisScale = capsuleCollider.direction == 0 ? Mathf.Abs(lossy.x) : capsuleCollider.direction == 1 ? Mathf.Abs(lossy.y) : Mathf.Abs(lossy.z);
            radius = capsuleCollider.radius * MaxAbsComponent(lossy);
            float halfCyl = Mathf.Max(0f, (capsuleCollider.height * 0.5f - capsuleCollider.radius)) * axisScale;

            Vector3 center = colliderTransform.TransformPoint(capsuleCollider.center);
            Vector3 axis =
                capsuleCollider.direction == 0 ? colliderTransform.right :
                capsuleCollider.direction == 1 ? colliderTransform.up :
                colliderTransform.forward;

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