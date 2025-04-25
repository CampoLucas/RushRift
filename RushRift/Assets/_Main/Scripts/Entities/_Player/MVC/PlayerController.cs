using Game.Entities.AttackSystem;
using Game.Entities.AttackSystem.Modules;
using Game.Entities.Components;
using Game.Inputs;
using Game.Predicates;
using Game.Utils;
using UnityEngine;

namespace Game.Entities
{
    public class PlayerController : EntityController
    {
        #region States

        public static HashedKey IdleState = new("Idle");
        public static HashedKey MoveState = new("Move");
        public static HashedKey RunState = new("Run");
        public static HashedKey JumpState = new("Jump");
        public static HashedKey FallState = new("Fall");
        public static HashedKey DieState = new("Die");

        #endregion

        [Header("Attacks")]
        [SerializeField] private StaticModuleData[] attacks;
        
        private Vector3 _moveDir;
        
        protected override void Awake()
        {
            base.Awake();
            EyesTransform = Camera.main.transform;
        }

        protected override void Start()
        {
            base.Start();
            
            if (GetModel().TryGetComponent<ComboHandler>(out var comboHandler))
            {
                for (var i = 0; i < attacks.Length; i++)
                {
                    var attack = attacks[i];
                    if (attack == null) continue;
                    
                    comboHandler.AddModule(attack.Test());
                }
            }
        }

        protected override void Update()
        {
            var inputDir = InputManager.GetValueVector(InputManager.MoveInput).XOZ();
            _moveDir = EyesTransform.forward * inputDir.z + EyesTransform.right * inputDir.x;
            _moveDir.y = 0;
            
            base.Update();
        }
        
        protected override void InitStateMachine()
        {
            _fsm = new EntityStateMachine(this);

            var playerModel = model as PlayerModelSO;
            
            var idleState = new IdleState();
            var moveState = new MoveState(playerModel.MoveSpeed);
            var jumpState = new JumpState(playerModel.Jump, playerModel.Gravity);
            var fallState = new FallState(playerModel.Gravity);

            _fsm.AddState(IdleState, idleState);
            _fsm.AddState(MoveState, moveState);
            _fsm.AddState(JumpState, jumpState);
            _fsm.AddState(FallState, fallState);
            
            _fsm.SetRootState(IdleState);
            _fsm.SetState(IdleState);

            // Idle Transitions
            idleState.AddTransition(MoveState, new IsMovingPredicate());
            idleState.AddTransition(JumpState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new IsGroundedPredicate(),
                new InputButtonPredicate<EntityArgs>(InputManager.JumpInput, InputButtonPredicate<EntityArgs>.State.Down)
            }));
            
            // Move Transitions
            moveState.AddTransition(IdleState, new IsMovingPredicate(false));
            moveState.AddTransition(JumpState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new IsGroundedPredicate(),
                new InputButtonPredicate<EntityArgs>(InputManager.JumpInput, InputButtonPredicate<EntityArgs>.State.Down)
            }));
            
            // Jump Transitions
            jumpState.AddTransition(FallState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new StateCompletedPredicate(),
                new IsGroundedPredicate(false),
            }));
            jumpState.AddTransition(MoveState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new StateCompletedPredicate(),
                new IsGroundedPredicate(),
                new IsMovingPredicate(),
            }));
            jumpState.AddTransition(IdleState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new StateCompletedPredicate(),
                new IsGroundedPredicate(),
                new IsMovingPredicate(false),
            }));
            
            // Fall Transitions
            fallState.AddTransition(MoveState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new IsGroundedPredicate(),
                new IsMovingPredicate(),
            }));
            fallState.AddTransition(IdleState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new IsGroundedPredicate(),
                new IsMovingPredicate(false),
            }));
            
            // Any State
            _fsm.AddAnyTransition(FallState, new CompositePredicate<EntityArgs>(new IPredicate<EntityArgs>[]
            {
                new CompareStatePredicate(JumpState, false),
                new IsGroundedPredicate(false),
            }));
        }

        public override Vector3 MoveDirection() => _moveDir;
    }
}