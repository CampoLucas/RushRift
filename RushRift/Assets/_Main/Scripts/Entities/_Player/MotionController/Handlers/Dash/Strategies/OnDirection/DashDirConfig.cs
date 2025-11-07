using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashDirConfig
    {
        public float Weight => weight;
        
        [SerializeField] private float weight = 1;
    }
}