using Game.Inputs;
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

        private Vector3 _moveDir;
        
        protected override void Awake()
        {
            base.Awake();
            EyesTransform = Camera.main.transform;
        }

        protected override void Update()
        {
            var inputDir = InputManager.GetValueVector(InputManager.MoveInput).XOZ();
            _moveDir = EyesTransform.forward * inputDir.z + EyesTransform.right * inputDir.x;
            _moveDir.y = 0;
            
            base.Update();
        }

        public override Vector3 MoveDirection() => _moveDir;
    }
}