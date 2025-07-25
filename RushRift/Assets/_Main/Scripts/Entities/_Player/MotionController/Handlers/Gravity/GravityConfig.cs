using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class GravityConfig : MotionConfig
    {
        public float GndGrav => groundGravity;
        public float HoverSpeed => hoverSpeed;
        public float GndDist => groundDistance;
        public float CorrDist => correctionDistance;
        public float FallGrav => fallGravity;
        public float MaxFallSpeed => maxFallSpeed;
        public float CurveDur => curveDuration;
        public float StartMult => startMultiplier;
        public float EndMult => endMultiplier;
        
        [Header("Gravity")]
        [SerializeField] private float groundGravity = 100f;
        [SerializeField] private float fallGravity = 600f;
        [Tooltip("Clamps the y velocity when falling")]
        [SerializeField] private float maxFallSpeed = 80f;

        [Header("Fall Curve")]
        [SerializeField] private float curveDuration = 1f;
        [SerializeField] private float startMultiplier = 1f;
        [SerializeField] private float endMultiplier = 2.5f;

        [Header("Hovering")]
        [Tooltip("The distance from the ground that the y position will be.")]
        [SerializeField] private float groundDistance = 0f;
        [Tooltip("The speed the Y position is adjusted.")]
        [SerializeField] private float hoverSpeed = 15f;
        [Tooltip("Used to disable the ground gravity is it is hovering.")]
        [SerializeField] private float correctionDistance = .5f;
        
        public override BaseMotionHandler GetHandler()
        {
            return new GravityHandler(this);
        }
    }
}