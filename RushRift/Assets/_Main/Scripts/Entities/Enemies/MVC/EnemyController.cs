using BehaviourTreeAsset.Runtime;
using Game.DesignPatterns.Observers;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities.Enemies.MVC
{
    public class EnemyController : EntityController
    {
        public static ISubject onEnemyDeathSubject = new Subject();
        public static ISubject onEnemySpawnSubject = new Subject();
        
        [Header("Eyes")]
        [SerializeField] private Transform eyes;
        
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("BehaviourTree")]
        [SerializeField] private BehaviourTreeRunner runner;
        [SerializeField] private int damageIndex;
        [SerializeField] private int deathIndex;

        private IObserver<(float, float, float)> _onDamageObserver;
        private IObserver _onDeathObserver;
        private EnemyComponent _enemyComp;
        
        protected override void Awake()
        {
            base.Awake();
            EyesTransform = eyes;

            _onDamageObserver = new ActionObserver<(float, float, float)>(OnDamage);
            _onDeathObserver = new ActionObserver(OnDeath);
            

        }

        protected override void Start()
        {
            base.Start();
            onEnemySpawnSubject.NotifyAll();
            if (target) Init(target);
        }

        public void Init(Transform newTarget)
        {
            Debug.Log("init");
            target = newTarget;
            if (GetModel().TryGetComponent(out _enemyComp))
            {
                _enemyComp.SetTarget(newTarget);
            }

            if (GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                LevelManager.GetEnemiesReference(healthComponent.OnValueDepleted);
                
                healthComponent.OnValueChanged.Attach(_onDamageObserver);
                healthComponent.OnValueDepleted.Attach(_onDeathObserver);
            }
        }

        public override Vector3 MoveDirection()
        {
            if ((_enemyComp != null || (_enemyComp == null && GetModel().TryGetComponent(out _enemyComp))) &&
                _enemyComp.TryGetTarget(out var t))
            {
                return (t.position - Transform.position).normalized;
            }
            
            return Vector3.zero;
        }

        private void OnDeath()
        {
            onEnemyDeathSubject.NotifyAll();
            runner.DisableAllRunners();
            runner.SetRunnerActive(deathIndex);
        }
        
        private void OnDamage((float, float, float) args)
        {
            if (args.Item1 >= args.Item2) return;
            
            runner.DisableAllRunners();
            runner.SetRunnerActive(damageIndex);
           
        }
    }
}