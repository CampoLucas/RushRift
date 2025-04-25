using Game.DesignPatterns.Observers;
using Game.DesignPatterns.Pool;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public class ProjectileProxy : ModuleProxy<ProjectileModule>
    {
        private IPoolObject<Projectile, ProjectileData> _pool;
        private IController _controller;
        private bool _executed;
        private float _timer;
        private NullCheck<ComboHandler> _handler;
        private LinearProjectileSpawner _forward;
        private DiagonalProjectileSpawner _diagonal;

        public ProjectileProxy(ProjectileModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            _pool = data;
            _controller = controller;
            if (_controller != null && _controller.GetModel().TryGetComponent<ComboHandler>(out var handler))
            {
                _handler = handler;
            }
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
            
            if (_timer < Data.Delay) return;
            
            _executed = true;
            OnDo(Data.GetOffsetPosition(mParams.Origin), mParams.Origin.rotation, mParams.Target.Get().Transform.gameObject);
        }
        

        private void OnDo(Vector3 position, Quaternion rotation, GameObject thrower)
        {
            if (!_handler || _handler.Get().ComboStats is not { } stats) return;

            var data = Data.PData;
            var newData = new ProjectileData(data.Damage, data.Speed, data.LifeTime, stats.Size, stats.PenetrationCount, stats.WallBounceCount, stats.EnemyBounceCount, stats.HasGravity);
            
            
            if (stats.ForwardAmount > 0)
            {
                _forward.Fire(stats.ForwardAmount, stats.ForwardDistance * stats.Size, position, rotation, Data.ForwardOffset, newData, thrower, _pool);
            }

            if (stats.DiagonalAmount > 0)
            {
                _diagonal.Fire(stats.DiagonalAmount + 1, stats.DiagonalAngle, position, rotation, Data.ForwardOffset, newData, thrower, _pool);
            }
            
            
            // var angleStep = Data.Amount <= 1 ? 0 : Data.Spread / (Data.Amount - 1);
            // var startAngle = Data.Amount <= 1 ? 0 : -Data.Spread / 2;
            //
            // //var rot = Quaternion.LookRotation(direction, upDirection);
            //
            // for (var i = 0; i < Data.Amount; i++)
            // {
            //     var angle = startAngle + (i * angleStep);
            //     var rot = Quaternion.Euler(0, angle, 0) * rotation;
            //
            //     var spawnPos = (rot * (Vector3.forward * Data.ForwardOffset)) + position;
            //
            //     var data = Data.PData;
            //     if (_handler && _handler.Get().ComboStats is { } stats)
            //     {
            //         var newData = new ProjectileData(data.Damage, data.Speed, data.LifeTime, stats.Size, stats.PenetrationCount, stats.WallBounceCount, stats.EnemyBounceCount, stats.HasGravity);
            //         Fire(spawnPos, rot, newData, thrower);
            //     }
            //     else
            //     {
            //         Fire(spawnPos, rot, data, thrower);
            //     }
            // }
        }
        
        private void Fire(Vector3 spawnPos, Quaternion rot, ProjectileData data, GameObject thrower)
        {
            
            
            var p = _pool.Get(spawnPos, rot, data);
            p.SetThrower(thrower);
        }

        protected override void OnDispose()
        {
            _pool = null;
        }
    }
}