using Game.VFX;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.AttackSystem.Hitscan
{
    public class HitscanModule : StaticModuleData
    {
        public float Delay => delay;
        public float Range => range <= 0 ? 100 : range;
        
        public bool AddSpread => addSpread;
        public float Spread => spread;
        public LayerMask GroundMask => groundMask;
        public LayerMask EntityMask => entityMask;
        public ParticleSystem Muzzle => muzzleEffect;
        public string ImpactID => impactEffectID;
        public float ImpactSize => impactSize;
        public ElectricArcController Line => line;
        public float LineDuration => lineDuration;
        public float Damage => damage;
        public float Radius => radius;
        public EntityJoint SpawnJoint => spawnJoint;
        public EntityJoint OriginJoint => originJoint;
        public Vector3 Offset => offset;
        public bool UseSFX => useSFX;
        public string SFXName => sfxName;

        [Header("Settings")]
        [SerializeField] private float damage = 10;
        
        [Header("Spawn")]
        [SerializeField] private Vector3 offset;
        //[SerializeField] private float forwardOffset;
        [SerializeField] private float delay = .5f;
        [SerializeField] private float range = -1;
        [SerializeField] private EntityJoint spawnJoint;
        [SerializeField] private EntityJoint originJoint;
        
        [Header("Spread")]
        [SerializeField] private bool addSpread = true;
        [SerializeField] private float spread = .1f;

        [FormerlySerializedAs("mask")]
        [Header("Collision")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask entityMask;
        [SerializeField] private float radius = .5f;

        [Header("Visuals")]
        [SerializeField] private ParticleSystem muzzleEffect;
        
        [SerializeField] private string impactEffectID = "Hit";
        [SerializeField] private float impactSize = 1;
        
        [Header("Line")]
        [SerializeField] private ElectricArcController line;
        [SerializeField] private float lineDuration;

        [Header("SFX")]
        [SerializeField] private bool useSFX;
        [SerializeField] private string sfxName;
        
        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new HitscanProxy(this, ChildrenProxies(controller));
        }

        public Vector3 GetOffsetPosition(Transform origin)
        {
            var x = origin.right * offset.x;
            var y = origin.up * offset.y;
            var z = origin.forward * offset.z;

            return origin.position + x + y + z;
        }

        public Vector3 GetDirection(Vector3 eyesPos, Vector3 forward, Vector3 spawnPos)
        {
            var direction = forward;
            
            if (Physics.Raycast(eyesPos, forward, out var hit, Range, GroundMask))
            {
                direction = (hit.point - spawnPos).normalized;
            }

            if (AddSpread)
            {
                // Get a random point in a unit circle
                var spread = Random.insideUnitCircle * Spread;
                
                // Build a rotation offset relative to the forward direction
                var spreadRotation = Quaternion.Euler(spread.y, spread.x, 0);
                direction = spreadRotation * direction;
            }
            
            return direction.normalized;
        }
        
        
    }
}