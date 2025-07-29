using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class DashConfig : MotionConfig
    {
        public float Force => force;
        public float Duration => duration;
        public float MomentumMult => momentumMultiplier;
        // public float Radius => radius;
        // public float Height => height;
        
        [Header("General")]
        [SerializeField] private float force = 120f;
        [SerializeField] private float duration = .25f;
        [SerializeField] private float momentumMultiplier = .25f;

        // [Header("Collision Prevention")]
        // [SerializeField] private float radius;
        // [SerializeField] private float height;
        
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            controller.TryAddHandler(new DashHandler(this), rebuildHandlers);
        }
    }
}