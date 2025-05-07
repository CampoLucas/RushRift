using UnityEngine;

namespace Game.Entities
{
    [System.Serializable]
    public class ProjectileData : IPrototype<ProjectileData>
    {
        public float Speed => speed;
        public float Size => size;
        public float Damage => damage;
        public float LifeTime => lifeTime;
        public int Penetration => penetrationCount;
        public int WallBounce => wallBounceCount;
        public int EnemyBounce => enemyBounceCount;
        public bool Gravity => hasGravity;
        
        [SerializeField] private float damage;
        [SerializeField] private float speed;
        [SerializeField] private float lifeTime;
        
        [SerializeField] private float size;

        [SerializeField] private int penetrationCount;
        [SerializeField] private int wallBounceCount;
        [SerializeField] private int enemyBounceCount;
        [SerializeField] private bool hasGravity;

        public ProjectileData(float damage, float speed, float lifeTime, float size, int penetration = 0, int wallBounce = 0, int enemyBounce = 0, bool gravity = false)
        {
            this.damage = damage;
            this.speed = speed;
            this.lifeTime = lifeTime;
            this.size = size;
            penetrationCount = penetration;
            wallBounceCount = wallBounce;
            enemyBounceCount = enemyBounce;
            hasGravity = gravity;
        }

        public ProjectileData Combine(ProjectileData other)
        {
#if false
            var isBouncy = other.Bouncy || bouncy;
            var hasGravity = other.Gravity || gravity;
            
            return new ProjectileData(
                other.Damage > damage ? other.Damage : damage,
                other.Speed > speed ? other.Speed : speed,
                other.LifeTime > lifeTime ? other.LifeTime : lifeTime,
                other.Size > size ? other.Size : size,
                isBouncy ? other.MaxCollision > maxCollision ? other.MaxCollision : maxCollision : 1,
                hasGravity, 
                isBouncy);
#else

            return new ProjectileData(
                other.Damage > damage ? other.Damage : damage,
                other.Speed > speed ? other.Speed : speed,
                other.LifeTime > lifeTime ? other.LifeTime : lifeTime,
                other.Size > size ? other.Size : size,
                other.Penetration > Penetration ? other.Penetration : Penetration,
                other.WallBounce > WallBounce ? other.WallBounce : WallBounce,
                other.EnemyBounce > EnemyBounce ? other.EnemyBounce : EnemyBounce,
                other.Gravity ? other.Gravity : Gravity);
#endif

            // Size: if size is diferent, the greater value, else the sum of both
            // Damage: if damage is diferent, the greter value, else the sum of both
        }

        public ProjectileData Clone()
        {
            
            var data = new ProjectileData(Damage, Speed, LifeTime, Size, Penetration, WallBounce, EnemyBounce, Gravity);
            return data;
        }
    }
}