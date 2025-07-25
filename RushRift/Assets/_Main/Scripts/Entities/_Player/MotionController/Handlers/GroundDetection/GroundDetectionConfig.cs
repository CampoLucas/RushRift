using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class GroundDetectionConfig : MotionConfig
    {
        public float Radius => radius;
        public float Offset => offset;
        public float Distance => distance;
        public LayerMask Layer => layer;
        
        [Header("Size & Offset")]
        [SerializeField] private float radius = .55f;
        [SerializeField] private float offset = 1.2f;
        
        [Header("Detection")]
        [SerializeField] private float distance = 1.25f;
        [SerializeField] private LayerMask layer;
        
        public override BaseMotionHandler GetHandler()
        {
            return new GroundDetectionHandler(this);
        }
    }
}