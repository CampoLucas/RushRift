using Game.Entities;
using UnityEngine;

namespace Game.DesignPatterns.Pool
{
    public class PoolManager : MonoBehaviour
    {
        private IPoolObject<Projectile, ProjectileData> _projectilePoolObject;
    }
}