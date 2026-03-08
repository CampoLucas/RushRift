using RushRift.Environment.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace RushRift.Environment
{
    public class ScaleModule : PlatformModule, IPlatUpdateModule
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Scale")]
        [Tooltip("What Axis from the target it scales. 0 = All, 1 = x, 2 = y, 3 = z")]
        [SerializeField] private int scaleAxis;
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 endScale = Vector3.one;

        [Header("Pivot")]
        [SerializeField] private bool catchTransform = true;
        [SerializeField] private Transform pivot;
        [SerializeField] private Vector3 pivotOffset;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float pivotRadius = 0.05f;
        [SerializeField] private bool drawPreview = true;

        private Vector3 _cachedPivotWorld;
        private Quaternion _cachedPivotRotation;
        private Vector3 _catchLocalPivot;
        
        public override void Initialize(PlatformController controller)
        {
            base.Initialize(controller);

            if (!target)
                target = controller.transform;

            var pivotTransform = pivot ? pivot : transform;

            target.localScale = startScale;

            _cachedPivotRotation = GetRotPivot(false);
            _cachedPivotWorld = GetWorldPivot(false);
            _catchLocalPivot = GetLocalPivot(false);
        }
        
        public void OnUpdate(float progress, bool inverse, float delta)
        {
            if (!target)
                return;

            var currentScale = EvaluateScale(progress);
            target.localScale = currentScale;
            target.localPosition = ComputeLocalPosition(currentScale);
        }

        private Quaternion GetRotPivot(bool useCached)
        {
            return useCached ? _cachedPivotRotation : (pivot ? pivot : transform).rotation;
        }

        private Vector3 GetWorldPivot(bool useCached)
        {
            return useCached
                ? _cachedPivotWorld
                : (pivot ? pivot : transform).position + (GetRotPivot(false) * pivotOffset);
        }

        private Vector3 GetLocalPivot(bool useCached)
        {
            return useCached ? _catchLocalPivot : target.InverseTransformPoint(GetWorldPivot(false));
        }
        
        private Vector3 EvaluateScale(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (scaleAxis <= 0)
                return Vector3.LerpUnclamped(startScale, endScale, progress);

            var scale = startScale;

            switch (scaleAxis)
            {
                case 1:
                    scale.x = Mathf.LerpUnclamped(startScale.x, endScale.x, progress);
                    break;

                case 2:
                    scale.y = Mathf.LerpUnclamped(startScale.y, endScale.y, progress);
                    break;

                default:
                    scale.z = Mathf.LerpUnclamped(startScale.z, endScale.z, progress);
                    break;
            }

            return scale;
        }

        private Vector3 ComputeLocalPosition(Vector3 scale)
        {
            var pivotInParentSpace = target.parent
                ? target.parent.InverseTransformPoint(GetWorldPivot(catchTransform))
                : GetWorldPivot(catchTransform);

            var scaledLocalPivot = Vector3.Scale(scale, GetLocalPivot(catchTransform));
            var rotatedScaledPivot = target.localRotation * scaledLocalPivot;

            return pivotInParentSpace - rotatedScaledPivot;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            var pivotTransform = pivot ? pivot : transform;
            var pivotWorld = pivotTransform.position + (pivotTransform.rotation * pivotOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pivotWorld, pivotRadius);

            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawLine(pivotTransform.position, pivotWorld);

            var axisSize = pivotRadius * 2f;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(pivotWorld, pivotWorld + pivotTransform.right * axisSize);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(pivotWorld, pivotWorld + pivotTransform.up * axisSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pivotWorld, pivotWorld + pivotTransform.forward * axisSize);

            if (!target || !drawPreview)
                return;

            var previewPivotLocalOnTarget = target.InverseTransformPoint(pivotWorld);

            var closedScale = GetPreviewScale(0f);
            var openScale = GetPreviewScale(1f);

            var closedLocalPos = ComputePreviewLocalPosition(target, pivotWorld, previewPivotLocalOnTarget, closedScale);
            var openLocalPos = ComputePreviewLocalPosition(target, pivotWorld, previewPivotLocalOnTarget, openScale);

            var closedWorldPos = target.parent ? target.parent.TransformPoint(closedLocalPos) : closedLocalPos;
            var openWorldPos = target.parent ? target.parent.TransformPoint(openLocalPos) : openLocalPos;

            var closedMatrix = Matrix4x4.TRS(closedWorldPos, target.rotation, closedScale);
            var openMatrix = Matrix4x4.TRS(openWorldPos, target.rotation, openScale);

            Gizmos.color = Color.green;
            Gizmos.matrix = closedMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.color = Color.yellow;
            Gizmos.matrix = openMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.identity;
        }

        private Vector3 GetPreviewScale(float progress)
        {
            if (scaleAxis <= 0)
                return Vector3.LerpUnclamped(startScale, endScale, progress);

            var scale = startScale;

            switch (scaleAxis)
            {
                case 1:
                    scale.x = Mathf.LerpUnclamped(startScale.x, endScale.x, progress);
                    break;

                case 2:
                    scale.y = Mathf.LerpUnclamped(startScale.y, endScale.y, progress);
                    break;

                default:
                    scale.z = Mathf.LerpUnclamped(startScale.z, endScale.z, progress);
                    break;
            }

            return scale;
        }

        private static Vector3 ComputePreviewLocalPosition(
            Transform target,
            Vector3 pivotWorld,
            Vector3 pivotLocalOnTarget,
            Vector3 scale)
        {
            Vector3 pivotInParentSpace = target.parent
                ? target.parent.InverseTransformPoint(pivotWorld)
                : pivotWorld;

            Vector3 scaledLocalPivot = Vector3.Scale(scale, pivotLocalOnTarget);
            Vector3 rotatedScaledPivot = target.localRotation * scaledLocalPivot;

            return pivotInParentSpace - rotatedScaledPivot;
        }
#endif
    }
}