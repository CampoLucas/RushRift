using BehaviourTreeAsset.Runtime;
using Game.DesignPatterns.Observers;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities
{
    public class EnemyController : EntityController
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("BehaviourTree")]
        [SerializeField] private BehaviourTreeRunner runner;
        [SerializeField] private int damageIndex;
        [SerializeField] private int deathIndex;
        
        private IObserver<float, float, float> _onDamageObserver;
        private IObserver _onDeathObserver;
        private EnemyComponent _enemyComp;
        
        protected override void Awake()
        {
            base.Awake();

            _onDamageObserver = new ActionObserver<float, float, float>(OnDamage);
            _onDeathObserver = new ActionObserver(OnDeath);
        }

        protected override void Start()
        {
            base.Start();
            LevelManager.OnEnemySpawnSubject.NotifyAll();
            if (target) Init(target);
        }

        public void Init(Transform newTarget)
        {
            target = newTarget;
            if (GetModel().TryGetComponent(out _enemyComp))
            {
                _enemyComp.SetTarget(newTarget);
            }

            if (GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                LevelManager.GetEnemiesReference(healthComponent.OnEmptyValue);
                
                healthComponent.OnValueChanged.Attach(_onDamageObserver);
                healthComponent.OnEmptyValue.Attach(_onDeathObserver);
            }
        }

        public override Vector3 MoveDirection()
        {
            if ((_enemyComp != null || (_enemyComp == null && GetModel().TryGetComponent(out _enemyComp))) &&
                _enemyComp.TryGetTarget(out var t))
            {
                return (t.position - Origin.position).normalized;
            }
            
            return Vector3.zero;
        }

        private void OnDeath()
        {
            AudioManager.Play("TurretDestruction");
            
            LevelManager.OnEnemyDeathSubject.NotifyAll();
            runner.DisableAllRunners();
            runner.SetRunnerActive(deathIndex);
        }
        
        private void OnDamage(float currentHealth, float previousHealth, float maxHealth)
        {
            if (currentHealth >= previousHealth) return;
            
            AudioManager.Play("TurretDamage");
            
            runner.DisableAllRunners();
            runner.SetRunnerActive(damageIndex);
           
        }
    }
}