using Game.DesignPatterns.Observers;
using Game.Entities;

namespace Game
{
    public static class GlobalEvents
    {
        public static readonly Subject<IController> EnemyDeath = new();
        public static readonly Subject<IController> EnemySpawned = new();
        public static readonly Subject<Projectile> ProjectileDestroyed = new();
        public static readonly Subject<bool> GameOver = new();
        public static readonly Subject LevelReset = new();
        public static readonly Subject<float> TimeUpdated = new();
        
        public static void Reset()
        {
            EnemyDeath.DetachAll();
            EnemySpawned.DetachAll();
            ProjectileDestroyed.DetachAll();
            GameOver.DetachAll();
            LevelReset.DetachAll();
            TimeUpdated.DetachAll();
        }
    }
}