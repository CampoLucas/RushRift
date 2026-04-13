using RushRift.Environment.Interfaces;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;

namespace RushRift.Environment
{
    public class MoveModule : PlatformModule, IPlatUpdateModule
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Positions")]
        [SerializeField] private bool useCurrentAsStart = false;
        [SerializeField, HideIf(nameof(useCurrentAsStart), true)] private Vector3 startPos;
        [SerializeField] private Vector3 endPos;

        [Header("Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float sphereRadius = .05f;
        

        public override void Initialize(PlatformController controller)
        {
            base.Initialize(controller);

            if (!target)
                target = controller.transform;

            if (useCurrentAsStart)
                startPos = target.localPosition;
        }

        public void OnUpdate(float progress, bool inverse, float delta)
        {
            if (!target) return;

            target.localPosition = Vector3.LerpUnclamped(startPos, endPos, progress);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || !target) return;

            
            var worldStart = target.parent ? target.parent.TransformPoint(startPos) : startPos;
            var worldEnd = target.parent ? target.parent.TransformPoint(endPos) : endPos;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(worldStart, worldEnd);
            Gizmos.DrawSphere(worldStart, sphereRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(worldEnd, sphereRadius);
        }
#endif
    }
}