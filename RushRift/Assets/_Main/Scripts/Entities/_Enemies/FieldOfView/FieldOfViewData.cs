using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    [System.Serializable]
    public class FieldOfViewData
    {
        [SerializeField] private FOVBuilder[] FOVs;

        public IPredicate<FOVParams> GetFOV(bool ifAny)
        {
            var predicates = new IPredicate<FOVParams>[FOVs.Length];
            
            for (var i = 0; i < FOVs.Length; i++)
            {
                predicates[i] = FOVs[i].GetFOV();
            }

            return new FieldOfView(predicates, ifAny);
        }

        public void Draw(Transform origin)
        {
            if (FOVs == null || FOVs.Length == 0) return;
            for (var i = 0; i < FOVs.Length; i++)
            {
                FOVs[i].Draw(origin, Color.red);
            }
        }
    }

    [System.Serializable]
    public class FOVBuilder
    {
        [Header("Angle")]
        [SerializeField] private bool checkAngle;
        [SerializeField] private float angle;
        
        [Header("Range")]
        [SerializeField] private bool checkRange;
        [SerializeField] private float range;
        
        [Header("View")]
        [SerializeField] private bool checkView;
        [SerializeField] private LayerMask mask;

        public IPredicate<FOVParams> GetFOV()
        {
            var predicates = new List<IPredicate<FOVParams>>();

            if (checkAngle)
            {
                predicates.Add(new CheckAngle(angle));
            }

            if (checkRange)
            {
                predicates.Add(new CheckRange(range));
            }

            if (checkView)
            {
                predicates.Add(new CheckView(mask));
            }

            return new FieldOfView(predicates.ToArray());
        }

        public void Draw(Transform origin, Color color)
        {
#if UNITY_EDITOR
            var forward = origin.forward;
            var position = origin.position;
            var a = checkAngle ? angle : 360;
            var r = checkRange ? range : 2;

            Gizmos.color = color;
            var halfFOV = a / 2f;
            var leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
            var rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);

            var leftRayDirection = leftRayRotation * forward;
            var rightRayDirection = rightRayRotation * forward;

            UnityEditor.Handles.color = color - new Color(0, 0, 0, 0.9f);
            if (checkAngle)
            {
                Gizmos.DrawRay(position, leftRayDirection * r);
                Gizmos.DrawRay(position, rightRayDirection * r);
                UnityEditor.Handles.DrawSolidArc(position, Vector3.up, leftRayDirection, a, r);
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireArc(position, Vector3.up, leftRayDirection, a, r);
            }
            else
            {
                UnityEditor.Handles.DrawSolidDisc(position, Vector3.up, r);
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireDisc(position, Vector3.up, r);
            }

            

#endif
        }
    }
}