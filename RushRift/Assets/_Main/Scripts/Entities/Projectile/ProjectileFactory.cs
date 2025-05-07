using Game.DesignPatterns.Factory;
using UnityEngine;

namespace Game.Entities
{
    public class ProjectileFactory : IFactory<Projectile, ProjectileData>
    {
        public Projectile Product { get; private set; }
        public bool Disposed { get; private set; }

        public ProjectileFactory(Projectile product)
        {
            Disposed = false;
            Product = product;
        }
        
        public Projectile Create()
        {
            var p = Object.Instantiate(Product);

            return p;
        }

        public Projectile[] Create(int quantity)
        {
            var projectiles = new Projectile[quantity];
            
            for (var i = 0; i < quantity; i++)
            {
                projectiles[i] = Object.Instantiate(Product);
            }

            return projectiles;
        }

        public void Dispose()
        {
            Disposed = true;
            Product = null;
        }
    }
}