using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Factory;
using Game.DesignPatterns.Pool;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Entities.AttackSystem.Modules
{
    // To do, make this have the pool instead of the proxy
    public class ProjectileModule : StaticModuleData, IFactory<Projectile, ProjectileData>
    {
        public Projectile Product => prefab;
        public int Amount => amount;
        public float Delay => delay;
        public Vector3 Offset => offset;
        public float ForwardOffset => forwardOffset;
        public ProjectileData PData => pData;
        
        [Header("Spawn Values")]
        [SerializeField] private Projectile prefab;
        [SerializeField] private int amount;
        [SerializeField] private float delay;

        [Header("Spawn Position")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private float forwardOffset;

        [Header("Projectile")]
        [SerializeField] private ProjectileData pData;

        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new ProjectileProxy(this, ChildrenProxies(controller), controller, disposeData);
        }

        public Projectile Create()
        {
            return Object.Instantiate(Product);
        }

        public Projectile[] Create(int quantity)
        {
            var projectiles = new Entities.Projectile[quantity];
            
            for (var i = 0; i < quantity; i++)
            {
                projectiles[i] = Object.Instantiate(Product);
            }

            return projectiles;
        }
        
        public Vector3 GetOffsetPosition(Transform origin)
        {
            var x = origin.right * offset.x;
            var y = origin.up * offset.y;
            var z = origin.forward * offset.z;

            return origin.position + x + y + z;
        }
    }
}