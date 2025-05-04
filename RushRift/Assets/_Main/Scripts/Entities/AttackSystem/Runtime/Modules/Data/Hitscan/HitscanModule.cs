using Game.VFX;
using UnityEngine;

namespace Game.Entities.AttackSystem.Hitscan
{
    public class HitscanModule : StaticModuleData
    {
        public float Delay => delay;
        public float Range => range <= 0 ? 100 : range;
        
        public bool AddSpread => addSpread;
        public float Spread => spread;
        public LayerMask Mask => mask;
        public ParticleSystem Muzzle => muzzleEffect;
        public ParticleSystem Impact => impactEffect;
        public ElectricArcController Trail => trail;
        
        [Header("Spawn")]
        [SerializeField] private Vector3 offset;
        //[SerializeField] private float forwardOffset;
        [SerializeField] private float delay = .5f;
        [SerializeField] private float range = -1;
        
        [Header("Spread")]
        [SerializeField] private bool addSpread = true;
        [SerializeField] private float spread = .1f;

        [Header("Collision")]
        [SerializeField] private LayerMask mask;

        [Header("Visuals")]
        [SerializeField] private ParticleSystem muzzleEffect;
        [SerializeField] private ParticleSystem impactEffect;
        [SerializeField] private ElectricArcController trail;
        
        
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
            
            if (Physics.Raycast(eyesPos, forward, out var hit, Range, Mask))
            {
                direction = (hit.point - spawnPos).normalized;
            }

            if (AddSpread)
            {
                direction += Vector3.one * Random.Range(-Spread, Spread);
                direction.Normalize();
            }
            
            return direction;
        }
    }
}