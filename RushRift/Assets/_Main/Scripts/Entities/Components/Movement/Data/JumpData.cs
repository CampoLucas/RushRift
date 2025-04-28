using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class JumpData
    {
        public float Height => height;
        public float MoveSpeed => moveSpeed;
        
        [SerializeField, Range(0f, 1f)] private float airControl = 0.5f;
        public float AirControl => airControl;


        
        [Header("Jump Settings")]
        [SerializeField] private float height;
        [SerializeField] private float moveSpeed;
        
        [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 1, 1, -1);
        [SerializeField] private float duration = 0.5f; // Total time of jump

        public AnimationCurve JumpCurve => jumpCurve;
        public float Duration => duration;
    }
}