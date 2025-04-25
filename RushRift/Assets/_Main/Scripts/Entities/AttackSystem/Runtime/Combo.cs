using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities.AttackSystem.Modules;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(fileName = "New ComboHandler", menuName = "Game/AttackSystem/ComboHandler")]
    public class Combo : ScriptableObject
    {
        [SerializeField] private List<Transition> attackTransitions;
        [SerializeField] private List<Transition> fromAnyTransitions;

        [Header("Debug")]
        [SerializeField] private Attack debugAttack;

        [Header("Stats")]
        [SerializeField] private ComboStats stats;
        //[SerializeField] private PropertyCollection<StaticModuleData> defaultModules;
        
        public void OnDraw(ComboHandler comboHandler)
        {
            if (debugAttack) debugAttack.OnDraw(comboHandler);
        }

        public ComboProxy GetProxy(IController controller)
        {
            var proxy = new ComboProxy(stats, attackTransitions, fromAnyTransitions, controller);

            // if (defaultModules != null && defaultModules.Count > 0)
            // {
            //     
            // }
            
            return proxy;
        }
    }

    public class ComboProxy : IDisposable
    {
        public List<TransitionProxy> StartTransitions { get; private set; }
        public List<TransitionProxy> FromAnyTransitions { get; private set; }
        public ComboStats Stats { get; private set; }

        private List<IModuleData> _data = new();
        private Dictionary<IModuleData, bool> _removableDictionary = new();
        private bool _modified;
        
        private List<IModuleProxy> _proxies = new();

        private ComboProxy(ComboStats stats)
        {
            Stats = stats.Clone();
        }
        
        public ComboProxy(ComboStats stats, List<TransitionProxy> startTransitions, List<TransitionProxy> fromAnyTransitions) : this(stats)
        {
            StartTransitions = startTransitions;
            FromAnyTransitions = fromAnyTransitions;
        }

        public ComboProxy(ComboStats stats, List<Transition> startTransitions, List<Transition> fromAnyTransitions, IController controller) : this(stats)
        {
            StartTransitions = startTransitions.Where(tr => tr != null).Select(tr => tr.GetProxy(controller)).ToList();
            FromAnyTransitions = fromAnyTransitions.Where(tr => tr != null).Select(tr => tr.GetProxy(controller)).ToList();
        }
        
        // public void SetDefaultModules()
        // {
        //     
        // }

        public void AddData(IModuleData data)
        {
            if (data == null) return;
            _data.Add(data);
            _modified = true;
        }

        public void RemoveData(IModuleData data)
        {
            if (data == null) return;
            if (!_data.Remove(data)) return;
            data.Dispose();
            _modified = true;
        }

        // when closing the inventory it would be executed
        public void BuildProxies(IController controller)
        {
            // Dispose all proxies
            for (var i = 0; i < _proxies.Count; i++)
            {
                _proxies[i].Dispose();
            }
            
            // Clear disposed proxies
            _proxies.Clear();
            
            // // Build new proxies

#if false
            for (var i = 0; i < _data.Count; i++)
            {
                if (!_data[i].Build(controller, _data, ref i, out var proxy)) continue;
                _proxies.Add(proxy);
                proxy.Init();
            }
#else
            IModuleData moduleData = null;
            
            for (var i = 0; i < _data.Count; i++)
            {
                var data = _data[i].Clone();
                
                if (moduleData == null)
                {
                    moduleData = data;
                }
                else if (moduleData.CanCombineData(data))
                {
                    var prevData = moduleData;
                    moduleData = moduleData.CombinedData(data);

                    prevData.Dispose();
                }
            }

            if (moduleData != null)
            {
                _proxies.Add(moduleData.GetProxy(controller, true));
                
            }
#endif

            

            _modified = false;
        }

        public List<IModuleProxy> GetProxies() => _proxies;
        
        public void Dispose()
        {
            Stats = null;
        }

        public void OnDraw(ComboHandler comboHandler)
        {
#if UNITY_EDITOR
            // var count = _proxies.Count;
            //
            // if (_proxies == null || count == 0) return;
            // for (var i = 0; i < count; i++)
            // {
            //     _proxies[i].OnDraw(comboHandler);
            // }
#endif
        }

        public void OnDrawSelected(ComboHandler comboHandler)
        {
#if UNITY_EDITOR
            // var count = _proxies.Count;
            //
            // if (_proxies == null || count == 0) return;
            // for (var i = 0; i < count; i++)
            // {
            //     _proxies[i].OnDrawSelected(comboHandler);
            // }
#endif
        }
    }

    [System.Serializable]
    public class ComboStats : IPrototype<ComboStats>
    {
        public bool CanAddModules => canAddModules;
        public int MaxModules => maxModules <= 0 ? 1 : maxModules;
        public float ModulesDelay => modulesDelay;
        public float Cooldown => cooldown;
        public float CostIncrease => costIncrease;
        public int MultiShotCount => multiShotCount;
        public int ForwardAmount => forwardAmount;
        public int DiagonalAmount => diagonalAmount;
        public int PenetrationCount => penetrationCount;
        public int WallBounceCount => wallBounceCount;
        public int EnemyBounceCount => enemyBounceCount;
        public bool HasGravity => gravity;
        public float Size => size;
        public float ForwardDistance => forwardDistance;
        public float DiagonalAngle => diagonalAngle;
        
        [Header("Customization")]
        [Tooltip("If the player can add/remove modules from the combo.")]
        [SerializeField] private bool canAddModules;
        [SerializeField] private int maxModules;

        [Header("Stats")]
        [Tooltip("The time it waits between executing each module.")]
        [SerializeField] private float modulesDelay;
        [Tooltip("The time it needs to wait after executing modules.")]
        [SerializeField] private float cooldown;
        [Tooltip("The amount in percentage that the cost is increased, if it is negative, the cost is reduced.")]
        [SerializeField] private float costIncrease;

        [Header("Attack Stats")]
        [Tooltip("How many times is fired per attack or simultaneously.")]
        [SerializeField] private int multiShotCount = 0;
        
        [Header("Forward Projectiles")]
        [Tooltip("How many projectiles are fired to the forward direction.")]
        [SerializeField] private int forwardAmount = 1;
        [SerializeField] private float forwardDistance = .25f;
        
        [Header("Diagonal Projectiles")]
        [Tooltip("How many projectiles are fired diagonally.")]
        [SerializeField] private int diagonalAmount = 0;
        [SerializeField] private float diagonalAngle = 45;

        [Header("Projectile Stats")]
        [Tooltip("If the projectile has gravity.")]
        [SerializeField] private bool gravity = false;
        [Tooltip("The size of the projectile.")]
        [SerializeField] private float size = 1;
        [Tooltip("How many targets a projectile can pierce.")]
        [SerializeField] private int penetrationCount = 0;
        [Tooltip("How many times it can bounce against walls.")]
        [SerializeField] private int wallBounceCount = 0;
        [Tooltip("How many times it bounces against other enemies.")]
        [SerializeField] private int enemyBounceCount = 0;

        private float _baseSize;

        public void IncreaseMultiShot(int amount) => multiShotCount += amount;
        public void DecreaseMultiShot(int amount) => multiShotCount -= amount;
        public void IncreaseForwardAmount(int amount) => forwardAmount += amount;
        public void DecreaseForwardAmount(int amount) => forwardAmount -= amount;
        public void IncreaseDiagonalAmount(int amount) => diagonalAmount += amount;
        public void DecreaseDiagonalAmount(int amount) => diagonalAmount -= amount;
        public void IncreasePenetration(int amount) => penetrationCount += amount;
        public void DecreasePenetration(int amount) => penetrationCount -= amount;
        public void IncreaseWallBounce(int amount) => wallBounceCount += amount;
        public void DecreaseWallBounce(int amount) => wallBounceCount -= amount;
        public void IncreaseEnemyBounce(int amount) => enemyBounceCount += amount;
        public void DecreaseEnemyBounce(int amount) => enemyBounceCount -= amount;
        public void SetHasGravity(bool value) => gravity = value;
        public void IncreaseSize(float percentage) => size += ((percentage * _baseSize) / 100);
        public void DecreaseDecrease(float percentage) => size -= ((percentage * _baseSize) / 100);


        public ComboStats Clone()
        {
            var stats = new ComboStats
            {
                canAddModules = canAddModules,
                maxModules = maxModules,
                modulesDelay = modulesDelay,
                cooldown = cooldown,
                costIncrease = costIncrease,
                multiShotCount = multiShotCount,
                penetrationCount = penetrationCount,
                wallBounceCount = wallBounceCount,
                enemyBounceCount = enemyBounceCount,
                gravity = gravity,
                forwardAmount = forwardAmount,
                diagonalAmount = diagonalAmount,
                size = size,
                _baseSize = size,
                forwardDistance = forwardDistance,
                diagonalAngle = diagonalAngle,
            };

            return stats;
        }
    }
}