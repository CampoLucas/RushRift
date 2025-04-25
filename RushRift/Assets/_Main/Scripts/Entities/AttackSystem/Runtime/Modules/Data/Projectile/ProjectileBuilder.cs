using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    [System.Serializable]
    public struct ProjectileTypeContainer
    {
        [Header("Prefab")]
        [SerializeField] private Entities.Projectile projectile;
        
        // ToDo: Effect ScriptableObjects
        //[Header("Effects")]
        //[SerializeField] private Effect
    }
    
    [CreateAssetMenu(menuName = "ModuleTesting/ProjectileBuilder")]
    public class ProjectileBuilder : ModuleBuilder<ProjectileModule>
    {
        [Header("Random Projectile Values")]
        [SerializeField] private Entities.Projectile[] projectiles;
        [SerializeField] private int[] amounts;
        [SerializeField] private int[] spreads;
        [SerializeField] private int[] damages;
        [SerializeField] private float[] speeds;
        [SerializeField] private float[] sizes;
        [SerializeField] private float[] lifeTimes;
        [SerializeField] private bool hasGravity;
        [SerializeField] private bool isBouncy;
        [SerializeField] private int maxCollisions = 1;

        [Header("Spawn Delay")]
        [SerializeField] private float spawnDelay;

        [Header("Offset")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private float forwardOffset;

        public override ProjectileModule GetModuleData()
        {
            var damage = damages[Random.Range(0, damages.Length)];
            var speed = speeds[Random.Range(0, speeds.Length)];
            var lifeTime = lifeTimes[Random.Range(0, lifeTimes.Length)];
            var size = sizes[Random.Range(0, sizes.Length)];
            var amount = amounts[Random.Range(0, amounts.Length)];
            var spread = spreads[Random.Range(0, spreads.Length)];
            var projectile = projectiles[Random.Range(0, projectiles.Length)];
            
            var projectileData = new ProjectileData(damage, speed, lifeTime, size, maxCollisions <= 0 ? 1 : maxCollisions);

            return new ProjectileModule(Children, Duration, projectile, projectileData, amount, spread,
                spawnDelay, forwardOffset, offset);
        }

        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return GetModuleData().GetProxy(controller, disposeData);
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