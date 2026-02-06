using System;
using UnityEngine;

namespace Game.UI.Elements
{
    public class PivotLookAt : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform target;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private Vector3 rotationOffset;

        [Header("Axis Locks")]
        [SerializeField] private bool lockX;
        [SerializeField] private bool lockY;
        [SerializeField] private bool lockZ;

        [Header("Angle Limits (Local)")]
        [SerializeField] private Vector3 minAngles = new Vector3(-80f, -180f, -180f);
        [SerializeField] private Vector3 maxAngles = new Vector3(80f, 180f, 180f);

        private Quaternion RotationOffset => Quaternion.Euler(rotationOffset);
        
        private void LateUpdate()
        {
            if (!pivot || !target) return;

            // We rotate THIS transform, but aim from the pivot position.
            var toTarget = target.position - pivot.position;
            if (toTarget.sqrMagnitude < 1e-6f) return;

            var desiredWorld = Quaternion.LookRotation(toTarget, transform.up);
            
            desiredWorld *= RotationOffset;

            // Convert desired world rotation into local rotation for clamping/locking.
            var parentWorld = transform.parent ? transform.parent.rotation : Quaternion.identity;
            var desiredLocal = Quaternion.Inverse(parentWorld) * desiredWorld;

            var desiredEuler = Normalize(desiredLocal.eulerAngles);
            var currentEuler = Normalize(transform.localEulerAngles);

            if (lockX) desiredEuler.x = currentEuler.x;
            if (lockY) desiredEuler.y = currentEuler.y;
            if (lockZ) desiredEuler.z = currentEuler.z;

            desiredEuler.x = Mathf.Clamp(desiredEuler.x, minAngles.x, maxAngles.x);
            desiredEuler.y = Mathf.Clamp(desiredEuler.y, minAngles.y, maxAngles.y);
            desiredEuler.z = Mathf.Clamp(desiredEuler.z, minAngles.z, maxAngles.z);

            var clampedLocal = Quaternion.Euler(desiredEuler);

            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                clampedLocal,
                rotationSpeed * Time.deltaTime
            );
        }

        public void SetPivot(Transform newTr)
        {
            pivot = newTr;
        }

        public void SetTarget(Transform newTr)
        {
            target = newTr;
        }

        private Vector3 Normalize(Vector3 e)
        {
            e.x = NormalizeAngle(e.x);
            e.y = NormalizeAngle(e.y);
            e.z = NormalizeAngle(e.z);
            return e;
        }

        private float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }

        private void OnDestroy()
        {
            pivot = null;
            target = null;
        }
    }
}