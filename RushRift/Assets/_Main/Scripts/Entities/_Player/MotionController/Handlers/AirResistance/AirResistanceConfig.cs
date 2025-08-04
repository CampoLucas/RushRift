using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class AirResistanceConfig : MotionConfig
    {
        public float AirResistance => airResistance;
        
        [Header("General")]
        [Tooltip("Used to scale the horizontal velocity while mid-air.")]
        [Range(0, 1)] [SerializeField] private float airResistance = .98f;

        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            controller.TryAddHandler(new AirResistanceHandler(this), rebuildHandlers);
        }
    }
}