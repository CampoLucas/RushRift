using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Inputs;
using Game.Predicates;
using Game.Utils;
using System.Collections.Generic;
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

        [Header("Start Effects")]
        [SerializeField] private List<int> effectsID = new List<int>();

        private Dictionary<int, Effect> effects = new();
        private Vector3 _moveDir;
        private Transform _camera;
        private SaveData saveData;
        
        protected override void Awake()
        {
            base.Awake();
            _camera = Camera.main.transform;

            if (_camera)
            {
                if (_camera.gameObject.TryGetComponent<JointsContainer>(out var cameraJoints))
                {
                    joints.AddJoint(cameraJoints.Joints);
                }
                
                
                joints.SetJoint(EntityJoint.Eyes, _camera);
            }

            saveData = SaveAndLoad.Load();

            var scriptableReference = ScriptableReference.Instance;

            if (scriptableReference)
            {
                for (int i = 0; i < ScriptableReference.Instance.effectsReferences.Count; i++)
                {
                    effects.Add(ScriptableReference.Instance.effectsReferences[i].ID, ScriptableReference.Instance.effectsReferences[i].effect);
                }
            }
            
        }

        protected override void Start()
        {
            base.Start();
            
            if (GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
            {
                LevelManager.GetPlayerReference(healthComponent.OnValueDepleted);
            }

            effectsID = saveData.GetActiveEffects();
            if (effects == null || effects.Count == 0) return;

            for (int i = 0; i < effectsID.Count; i++)
            {
                var currentEffect = effects[effectsID[i]];
                currentEffect.ApplyEffect(this);
            }

            //for (var i = 0; i < effects.Length; i++)
            //{
            //    var effect = effects[i];
            //    if (effect == null) continue;
            //    effect.ApplyEffect(this);
            //}
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
            
            var idleState = new IdleState();
            var moveState = new MoveState();
            var jumpState = new JumpState(playerModel.Jump);
            var fallState = new MoveState();

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