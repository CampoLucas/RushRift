using Game.DesignPatterns.Observers;
using Game.DesignPatterns.Pool;
using Game.Utils;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Entities.AttackSystem
{
    public class ProjectileProxy : ModuleProxy<ProjectileModule>
    {
        private IPoolObject<Projectile, ProjectileData> _pool;
        private bool _executed;
        private float _timer;

        public ProjectileProxy(ProjectileModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            _pool = new PoolObject<Projectile, ProjectileData>(data);
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnReset);
            UpdateObserver = new ActionObserver<ModuleParams, float>(OnUpdate);
        }

        private void OnReset(ModuleParams mParams)
        {
            _executed = false;
            _timer = 0;
        }

        private void OnUpdate(ModuleParams mParams, float delta)
        {
            if (_executed) return;
            _timer += delta;
            
            if (_timer <= Data.Delay) return;
            
            _executed = true;
            OnDo(mParams.Joints.GetJoint(Data.SpawnJoint), mParams.Joints.GetJoint(Data.OriginJoint).rotation, mParams.Owner.Get().Origin.gameObject);
        }
        
        private void OnDo(Transform spawnPos, Quaternion rotation, GameObject thrower)
        {
            var data = Data.PData;
            
            AudioManager.Play("TurretFire");
            FireForwarlly(spawnPos, rotation, Data.Offset, Data.Amount, Data.Spacing * data.Size, Data.ForwardOffset, data, thrower, _pool);
        }
        
        public void FireForwarlly(Transform origin, Quaternion rotation, Vector3 offset, int amount, float spacing, float forwardOffset, ProjectileData pData, GameObject thrower, IPoolObject<Projectile, ProjectileData> pool)
        {
            var lessOrOne = amount <= 1;
            var totalWidth = spacing * (amount - 1);
            var startOffset = lessOrOne ? 0 : -totalWidth / 2;

            for (var i = 0; i < amount; i++)
            {
                // Calculate the offset along the local X-axis
                var xOffset = startOffset + (i * spacing);
                var bulletsOffset = new Vector3(xOffset, 0, forwardOffset);

                // Transform local offset to world space
                var worldOffset = rotation * bulletsOffset;
                var spawnPosition = origin.GetOffsetPos(offset) + worldOffset;

                var p = pool.Get(spawnPosition, rotation, pData);
                
                SceneManager.MoveGameObjectToScene(p.gameObject, thrower.scene);
                p.SetThrower(thrower);
                
            }
        }

        protected override void OnDispose()
        {
            _pool.Dispose();
            _pool = null;
        }
    }
}