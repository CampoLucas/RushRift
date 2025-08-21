using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashDamageConfig
    {
        public bool InstaKill => instaKill; 
        public float Damage => damage;
        //[field:SerializeField] public SphereOverlapData DetectionData { get; private set; }
        public bool StopOnKilling => stopOnDamage;
        public bool StopOnDestroy => stopOnDestroy;

        [Header("Entities w/ health")]
        [SerializeField] private bool instaKill = true;
        [SerializeField] private float damage = 100f;
        [SerializeField] private bool stopOnDamage = true;
        
        [Header("Destroyable entities")]
        [SerializeField] private bool stopOnDestroy = true;
    }
}