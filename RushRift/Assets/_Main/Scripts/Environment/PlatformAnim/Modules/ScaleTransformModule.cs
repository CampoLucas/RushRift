using System;
using Game.Utils;
using RushRift.Environment.Interfaces;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace RushRift.Environment
{
    public class ScaleTransformModule : PlatformModule, IPlatUpdateModule
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Scale")]
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 endScale = Vector3.one;

        [Header("Pivot")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Vector3 pivotOffset;
        
        [Header("Orientation")]
        [SerializeField] private bool useCustomOrientation;
        [SerializeField, HideIf(nameof(useCustomOrientation), false)] private Transform orientation;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float gizmoSphereRadius = 0.08f;
        
        private Vector3 _pivotPointLocalOnTarget ;
        private Transform _resolvedPivot;

        public override void Initialize(PlatformController controller)
        {
            base.Initialize(controller);

            if (!target)
            {
                target = controller.transform;
            }

            _resolvedPivot = pivot ? pivot : transform;
            
            Vector3 pivotWorldPoint = GetPivotWorldPoint();
            _pivotPointLocalOnTarget = target.InverseTransformPoint(pivotWorldPoint);
        }

        public void OnUpdate(float progress, bool inverse, float delta)
        {
            if (!target)
                return;

            Vector3 currentScale = Vector3.LerpUnclamped(startScale, endScale, progress);
            target.localScale = currentScale;

            Vector3 desiredPivotWorld = GetPivotWorldPoint();
            Vector3 desiredPivotInParentSpace = target.parent
                ? target.parent.InverseTransformPoint(desiredPivotWorld)
                : desiredPivotWorld;

            Quaternion localRotation = target.localRotation;

            // parentSpacePivotPoint = localPosition + (localRotation * scaledLocalPivotPoint)
            // localPosition = parentSpacePivotPoint - (localRotation * scaledLocalPivotPoint)
            Vector3 scaledPivotVector = Vector3.Scale(currentScale, _pivotPointLocalOnTarget);
            Vector3 rotatedScaledPivotVector = localRotation * scaledPivotVector;

            target.localPosition = desiredPivotInParentSpace - rotatedScaledPivotVector;
        }
        
        private float SafeDivide(float a, float b)
        {
            return Mathf.Approximately(b, 0f) ? 0f : a / b;
        }
        
        private Vector3 GetPivotWorldPoint()
        {
            Transform pivotTransform = pivot ? pivot : transform;
            Transform offsetOrientation = GetOffsetOrientationTransform(pivotTransform);

            return pivotTransform.position + offsetOrientation.TransformDirection(pivotOffset);
        }

        private Transform GetOffsetOrientationTransform(Transform fallback)
        {
            if (useCustomOrientation && orientation)
                return orientation;

            return fallback;
        }
        
        private Vector3 ComputeLocalPositionForScale(Vector3 scale)
        {
            Vector3 pivotWorld = GetPivotWorldPoint();

            Vector3 pivotInParent = target.parent
                ? target.parent.InverseTransformPoint(pivotWorld)
                : pivotWorld;

            Quaternion rot = target.localRotation;

            Vector3 scaledPivot = Vector3.Scale(scale, _pivotPointLocalOnTarget);
            Vector3 rotated = rot * scaledPivot;

            return pivotInParent - rotated;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            var pivotTransform = pivot ? pivot : transform;
            var offsetOrientation = GetOffsetOrientationTransform(pivotTransform);

            var pivotWorldPoint = pivotTransform.position + offsetOrientation.TransformDirection(pivotOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(pivotWorldPoint, gizmoSphereRadius);

            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(pivotTransform.position, pivotWorldPoint);

            var axisSize = gizmoSphereRadius * 2f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pivotWorldPoint, pivotWorldPoint + offsetOrientation.right * axisSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pivotWorldPoint, pivotWorldPoint + offsetOrientation.up * axisSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pivotWorldPoint, pivotWorldPoint + offsetOrientation.forward * axisSize);

            if (target)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(target.position, pivotWorldPoint);
            }
        }
#endif
    }
}