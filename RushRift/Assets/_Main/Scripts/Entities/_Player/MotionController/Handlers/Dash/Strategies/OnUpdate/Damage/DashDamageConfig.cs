using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashDamageConfig
    {
        [field:SerializeField] public bool InstaKill { get; private set; }
        [field:SerializeField] public float Damage { get; private set; }
        [field:SerializeField] public SphereOverlapData DetectionData { get; private set; }
    }
}