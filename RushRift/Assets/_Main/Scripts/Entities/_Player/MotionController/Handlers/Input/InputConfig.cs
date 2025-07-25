using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public class InputConfig : MotionConfig
    {
        public float Sens => sensitivity;
        public float SensMult => sensitivityMultiplier;
        
        [Header("Sensitivity")]
        [SerializeField] private float sensitivity = 50;
        [SerializeField] private float sensitivityMultiplier = 1;
        
        public override BaseMotionHandler GetHandler()
        {
            return new InputHandler(this);
        }
    }
}