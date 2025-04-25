using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public struct LinearProjectileSpawner
    {
        public void Fire(int amount, float spacing, Vector3 position, Quaternion rotation, float forwardOffset, ProjectileData pData, GameObject thrower, IPoolObject<Projectile, ProjectileData> pool)
        {
            var lessOrOne = amount <= 1;
            var totalWidth = spacing * (amount - 1);
            var startOffset = lessOrOne ? 0 : -totalWidth / 2;

            for (var i = 0; i < amount; i++)
            {
                // Calculate the offset along the local X-axis
                var xOffset = startOffset + (i * spacing);
                var offset = new Vector3(xOffset, 0, forwardOffset);

                // Transform local offset to world space
                var worldOffset = rotation * offset;
                var spawnPosition = position + worldOffset;

                var p = pool.Get(spawnPosition, rotation, pData);
                p.SetThrower(thrower);
            }
        }
    }
}