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
        private Vector3 _localPivot;
        
        public override void Initialize(PlatformController controller)
        {
            base.Initialize(controller);

            if (!target) target = controller.transform;

            // Get the Pivot in World Space once
            var pivotTransform = pivot ? pivot : transform;
            var worldPivot = pivotTransform.position + (pivotTransform.rotation * pivotOffset);

            // We must record where the pivot is relative to the target 
            _localPivot = target.InverseTransformPoint(worldPivot);

            target.localScale = EvaluateScale(controller.StartProgress);
        }
        
        public void OnUpdate(float progress, bool inverse, float delta)
        {
            if (!target) return;

            var currentScale = EvaluateScale(progress);
        
            // Apply scale first
            target.localScale = currentScale;

            // Update position so the pivot point appears stationary in local space
            target.localPosition = ComputeLocalPosition(currentScale);
        }
        
        private Vector3 EvaluateScale(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            var start = startScale;
            var curr = target.localScale;
            var end = endScale;

            return scaleAxis switch
            {
                1 => new Vector3(Mathf.LerpUnclamped(start.x, end.x, progress), curr.y, curr.z),
                2 => new Vector3(curr.x, Mathf.LerpUnclamped(start.y, end.y, progress), curr.z),
                3 => new Vector3(curr.x, curr.y, Mathf.LerpUnclamped(start.z, end.z, progress)),
                _ => Vector3.LerpUnclamped(start, end, progress)
            };
        }

        private Vector3 ComputeLocalPosition(Vector3 scale)
        {
            // Calculate where the pivot is in the target's local space right now
            var scaledLocalP = Vector3.Scale(scale, _localPivot);
        
            // Convert to the parent's space
            var rotScaledP = target.localRotation * scaledLocalP;
        
            // If the pivot is a child of the same platform, its localPosition is constant
            var p = GetWorldPivot(catchTransform);
            if (target.parent)
            {
                p = target.parent.InverseTransformPoint(p);
            }

            return p - rotScaledP;
        }

        private Vector3 GetWorldPivot(bool useCached)
        {
            // If moving, always fetch the live position of the pivot transform
            var p = (pivot ? pivot : transform);
            return p.position + (p.rotation * pivotOffset);
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
            return EvaluateScale(progress);
        }

        private static Vector3 ComputePreviewLocalPosition(Transform target, Vector3 pivotWorld, Vector3 pivotLocalOnTarget, Vector3 scale)
        {
            if (!target) return Vector3.zero;
            
            var p = target.parent ? target.parent.InverseTransformPoint(pivotWorld) : pivotWorld;

            var scaledLocalPivot = Vector3.Scale(scale, pivotLocalOnTarget);
            var rotatedScaledPivot = target.localRotation * scaledLocalPivot;

            return p - rotatedScaledPivot;
        }
#endif
        
    }
}