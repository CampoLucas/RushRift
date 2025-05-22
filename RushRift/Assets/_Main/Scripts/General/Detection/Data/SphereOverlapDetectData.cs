using UnityEngine;

namespace Game.Detection
{
    [System.Serializable]
    public class SphereOverlapDetectData : IDetectionData
    {
        public int MaxCollisions => maxCollisions;
        
        [Header("Size & Offset")] 
        [SerializeField] private Vector3 offset;
        [SerializeField] private float radius;

        [Header("Detection Settings")]
        [SerializeField] private int maxCollisions;
        [SerializeField] private LayerMask mask;

        public IDetection Get(Transform origin) => new OverlapDetect(origin, this);

        public Vector3 GetPosOffset(Transform origin)
        {
            var x = origin.right * offset.x;
            var y = origin.up * offset.y;
            var z = origin.forward * offset.z;

            return origin.transform.position + x + y + z;
        }

        public int Detect(Transform origin, ref Collider[] colliders)
        {
            return Physics.OverlapSphereNonAlloc(GetPosOffset(origin), radius, colliders, mask);
            //return Physics.OverlapBoxNonAlloc(GetPosOffset(origin), size / 2, colliders, origin.rotation, mask);
        }

        public void Draw(Transform origin, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(GetPosOffset(origin), radius);
        }

    }
}