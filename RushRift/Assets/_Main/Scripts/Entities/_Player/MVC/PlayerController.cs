using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Inputs;
using Game.Predicates;
using Game.Utils;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class PlayerController : EntityController
    {
        #region States

        //public static HashedKey IdleState = new("Idle");
        public static HashedKey MoveState = new("Move");
        public static HashedKey RunState = new("Run");
        public static HashedKey JumpState = new("Jump");
        public static HashedKey FallState = new("Fall");
        public static HashedKey DieState = new("Die");

        #endregion

        [SerializeField] private Effect[] startEffects;

        private Vector3 _moveDir;
        private Transform _camera;

        private IObserver<float, float, float> _onPlayerDamage;
        
        protected override void Awake()
        {
            base.Awake();
            
            
            
            _onPlayerDamage = new ActionObserver<float, float, float>(OnPlayerDamage);
        }

        protected override void Start()
        {        
            base.Start();
            
            if (GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                healthComponent.OnValueChanged.Attach(_onPlayerDamage);
                LevelManager.GetPlayerReference(healthComponent.OnEmptyValue);
            }

            var saveData = SaveAndLoad.Load();

            if (saveData == null) return;

            var medalEffects = saveData.LevelsMedalsTimes;
            var currentLevel = LevelManager.GetCurrentLevel();
            Effect currentEffect;
            
            for (var i = 0; i < startEffects.Length; i++)
            {
                var effect = startEffects[i];
                if (effect.IsNullOrMissingReference()) continue;
                
                effect.ApplyEffect(this);
            }
            
            if (currentLevel == 0) return;

            if (medalEffects.TryGetValue(currentLevel, out var medalTimes))
            {
                if (medalTimes.bronze.isAcquired)
                {
                    currentEffect = LevelManager.GetEffect(medalEffects[currentLevel].bronze.upgrade);
                    currentEffect.ApplyEffect(this);
                }

                if (medalTimes.silver.isAcquired)
                {
                    currentEffect = LevelManager.GetEffect(medalEffects[currentLevel].silver.upgrade);
                    currentEffect.ApplyEffect(this);
                }

                if (medalTimes.gold.isAcquired)
                {
                    currentEffect = LevelManager.GetEffect(medalEffects[currentLevel].gold.upgrade);
                    currentEffect.ApplyEffect(this);
                }
            }
            
        }

        protected override void SetJoins()
        {
            _camera = Camera.main.transform;

            if (!_camera) return;
            if (_camera.gameObject.TryGetComponent<CameraData>(out var camData))
            {
                joints.AddJoint(camData.JointsContainer.Joints);

                animator = new[] { camData.ArmsAnimator };
            }
                
                
            //joints.SetJoint(EntityJoint.Eyes, _camera);
        }

        protected override void Update()
        {
            var inputDir = InputManager.GetValueVector(InputManager.MoveInput).XOZ();
            _moveDir = _camera.forward * inputDir.z + _camera.right * inputDir.x;
            _moveDir.y = 0;
            
            base.Update();
        }
        
        protected override void InitStateMachine()
        {
            _fsm = new EntityStateMachine(this);

            var playerModel = model as PlayerModelSO;
            
            //var idleState = new IdleState();
            var moveState = new MoveState(MoveType.Grounded);
            var jumpState = new JumpState(playerModel.Jump, MoveType.Air);
            var fallState = new MoveState(MoveType.Air);

            //_fsm.AddState(IdleState, idleState);
            _fsm.AddState(MoveState, moveState);
            _fsm.AddState(JumpState, jumpState);
            _fsm.AddState(FallState, fallState);
            
            //_fsm.SetRootState(IdleState);
            _fsm.SetState(MoveState);

            // // Idle Transitions
            // // idleState.AddTransition(MoveState, new IsMovingPredicate());
            // // idleState.AddTransition(JumpState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // // {
            // //     new IsGroundedPredicate(),
            // //     new InputButtonPredicate<EntityArgs>(InputManager.JumpInput, InputButtonPredicate<EntityArgs>.State.Down)
            // // }));
            //
            // // Move Transitions
            // //moveState.AddTransition(IdleState, new IsMovingPredicate(false));
            // moveState.AddTransition(JumpState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // {
            //     new IsGroundedPredicate(),
            //     new InputButtonPredicate<EntityArgs>(InputManager.JumpInput, InputButtonPredicate<EntityArgs>.State.Down)
            // }));
            //
            // // Jump Transitions
            // jumpState.AddTransition(FallState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // {
            //     new StateCompletedPredicate(),
            //     new IsGroundedPredicate(false),
            // }));
            // jumpState.AddTransition(MoveState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // {
            //     new StateCompletedPredicate(),
            //     new IsGroundedPredicate(),
            //     //new IsMovingPredicate(),
            // }));
            // // jumpState.AddTransition(IdleState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // // {
            // //     new StateCompletedPredicate(),
            // //     new IsGroundedPredicate(),
            // //     new IsMovingPredicate(false),
            // // }));
            //
            // // Fall Transitions
            // fallState.AddTransition(MoveState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // {
            //     new IsGroundedPredicate(),
            //     //new IsMovingPredicate(),
            // }));
            // // fallState.AddTransition(IdleState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // // {
            // //     new IsGroundedPredicate(),
            // //     new IsMovingPredicate(false),
            // // }));
            //
            // // Any State
            // _fsm.AddAnyTransition(FallState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            // {
            //     new CompareStatePredicate(JumpState, false),
            //     new IsGroundedPredicate(false),
            // }));
        }

        public override Vector3 MoveDirection() =>
            _moveDir;
        
        
        public void OnPlayerDamage(float previousValue, float newValue, float delta)
        {
            Debug.Log("Taking damage");
            
            AudioManager.Play("Grunt");
            //ScreenFlash.Instance.TriggerFlash("#FF0044", .1f, .1f);
        }
        
    }
}