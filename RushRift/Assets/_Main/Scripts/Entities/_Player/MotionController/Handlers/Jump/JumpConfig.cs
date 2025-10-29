using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class JumpConfig : MotionConfig
    {
        public float Force => force;
        public float Cooldown => cooldown;
        public float UpInfluence => upInfluence;
        public float InputInfluence => inputInfluence;
        public float VelocityInfluence => velocityInfluence;
        public float NormalInfluence => normalInfluence;
        public double MinHorVelocity => minHorizontalVelocity;

        [Header("General")]
        [SerializeField] private float force = 8f;
        [SerializeField, Range(0, 10)] private float upInfluence = 3;
        [SerializeField, Range(0, 10)] private float inputInfluence = 1;
        [FormerlySerializedAs("velocityDirInfluence")] [SerializeField, Range(0, 10)] private float velocityInfluence = 1;
        [SerializeField] private float minHorizontalVelocity = 5;
        [SerializeField, Range(0, 10)] private float normalInfluence = .5f;
        [SerializeField] private float cooldown = .25f;
        
        public override void AddHandler(in MotionController controller, in bool rebuildHandlers)
        {
            controller.TryAddHandler(new JumpHandler(this), rebuildHandlers);
        }
    }
}