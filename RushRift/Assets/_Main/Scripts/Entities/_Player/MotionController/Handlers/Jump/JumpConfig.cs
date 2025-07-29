using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class JumpConfig : MotionConfig
    {
        public float Force => force;
        public float Cooldown => cooldown;
        
        [Header("General")]
        [SerializeField] private float force = 8f;
        [SerializeField] private float cooldown = .25f;
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            controller.TryAddHandler(new JumpHandler(this), rebuildHandlers);
        }
    }
}