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
        public static ISubject OnEnemyDeathSubject = new Subject(); // ToDo: Move it to the a EnemyManager and dispose of all references
        public static ISubject OnEnemySpawnSubject = new Subject();
        public static ISubject<int> OnEnemyGivesPoints = new Subject<int>();
        
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("BehaviourTree")]
        [SerializeField] private BehaviourTreeRunner runner;
        [SerializeField] private int damageIndex;
        [SerializeField] private int deathIndex;

        [Header("Points")]
        [SerializeField] private int points;

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
            OnEnemySpawnSubject.NotifyAll();
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
                return (t.position - Origin.position).normalized;
            }
            
            return Vector3.zero;
        }

        private void OnDeath()
        {
            AudioManager.Play("TurretDestruction");
            
            OnEnemyDeathSubject.NotifyAll();
            OnEnemyGivesPoints.NotifyAll(points);
            runner.DisableAllRunners();
            runner.SetRunnerActive(deathIndex);
            OnEnemyDeathSubject.DetachAll();
            OnEnemySpawnSubject.DetachAll();

        }
        
        private void OnDamage(float currentHealth, float previousHealth, float maxHealth)
        {
            if (currentHealth >= previousHealth) return;
            
            AudioManager.Play("TurretDamage");
            runner.DisableAllRunners();
            runner.SetRunnerActive(damageIndex);
           
        }

        protected override void OnDispose()
        {
            OnEnemyDeathSubject.DetachAll();
            OnEnemySpawnSubject.DetachAll();
        }
    }
}