using System;
using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public struct DiagonalProjectileSpawner
    {
        public void Fire(int amount, float pDistance, Vector3 position, Quaternion rotation, float forwardOffset, ProjectileData pData, GameObject thrower, IPoolObject<Projectile, ProjectileData> pool)
        {
            if (amount <= 0)
                return;
            
            var lessOrOne = amount <= 1;
            
            var skipMiddle = amount >= 3 && amount % 2 == 1;
            var angleStep = lessOrOne ? 0 : pDistance / (amount - 1);
            var startAngle = lessOrOne ? 0 : -pDistance / 2;

            for (var i = 0; i < amount; i++)
            {
                if (skipMiddle && i == amount / 2)
                    continue;
                
                var angle = startAngle + (i * angleStep);
                //var rot = Quaternion.Euler(0, angle, 0) * rotation;
                var rot = Quaternion.AngleAxis(angle, rotation * Vector3.up) * rotation;
                var pos = (rot * (Vector3.forward * forwardOffset)) + position;

                var p = pool.Get(pos, rot, pData);
                p.SetThrower(thrower);
            }
        }
    }
}