using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class JumpData
    {
        public float Force => force;
        public float Duration => duration;
        public AnimationCurve Curve => curve;
        
        [SerializeField] private float force = 10;
        [SerializeField] private float duration = 0.5f; // Total time of jump
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 1, 1, -1);
    }
}