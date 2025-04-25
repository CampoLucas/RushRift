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
    public class ProjectileModule : RuntimeModuleData, IFactory<Projectile, ProjectileData>, IPoolObject<Projectile, ProjectileData>
    {
        public Projectile Product { get; private set; }
        public ProjectileData PData { get; private set; }
        public int Amount { get; private set; }
        public float Spread { get; private set; }
        public float Delay { get; private set; }
        public float ForwardOffset { get; private set; }
        private readonly Vector3 _offset;
        private IPoolObject<Projectile, ProjectileData> _pool;

        public ProjectileModule(List<IModuleData> children, float duration, Projectile projectile, ProjectileData pData,
            int amount, float spread, float delay, float forwardOffset, Vector3 offset) : base(children, duration)
        {
            Product = projectile;
            PData = pData;
            Amount = amount;
            Spread = spread;
            Delay = delay;
            ForwardOffset = forwardOffset;
            _offset = offset;
            _pool = new PoolObject<Projectile, ProjectileData>(this);
        }

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
            var x = origin.right * _offset.x;
            var y = origin.up * _offset.y;
            var z = origin.forward * _offset.z;

            return origin.position + x + y + z;
        }

        protected override void OnDispose()
        {
            _pool.Dispose();
            _pool = null;
            
            Product = null;
            PData = null;
        }

        public override bool CanCombineData(IModuleData data2)
        {
            return data2 is ProjectileModule/* or EffectModule*/;
        }

        public override IModuleData CombinedData(IModuleData data)
        {
            if (data is ProjectileModule otherProjectile)
            {
                var amount = otherProjectile.Amount > Amount ? otherProjectile.Amount : Amount;

                var product = otherProjectile.PData.WallBounce > 0 ? otherProjectile.Product : Product;
                
                return new ProjectileModule(ClonedChildren(), Duration, product, PData.Combine(otherProjectile.PData), amount,
                    (Spread + otherProjectile.Spread) * .5f, Delay, ForwardOffset, _offset);
            }
            // else if (data is EffectModule otherEffect)
            // {
            //     var newData = Clone();
            //     newData.AddEffects(otherEffect.GetEffects());
            // }
            
            return base.CombinedData(data);
        }

        public override IModuleData Clone()
        {
            return new ProjectileModule(ClonedChildren(), Duration, Product, PData, Amount, Spread, Delay, ForwardOffset,
                _offset);
        }
        
        // public override IModuleData Clone()
        // {
        //     return Clone()
        // }

        public void Recycle(Projectile poolable)
        {
            _pool.Recycle(poolable);
        }

        public void Remove(Projectile poolable)
        {
            _pool.Remove(poolable);
        }

        public bool TryGet(Vector3 position, Quaternion rotation, ProjectileData data, out Projectile poolable)
        {
            return _pool.TryGet(position, rotation, data, out poolable);
        }

        public Projectile Get(Vector3 position, Quaternion rotation, ProjectileData data)
        {
            return _pool.Get(position, rotation, data);
        }
    }
}