using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class JumpState : State<EntityArgs>
    {
        private JumpData _jumpData;
        private GravityData _gravityData;

        private float _gravity;
        private float _jumpForce;
        private float _velocity;
        private float _elapsedTime;

        public JumpState(JumpData jumpData, GravityData gravityData)
        {
            _jumpData = jumpData;
            _gravityData = gravityData;
        }

        protected override void OnInit(ref EntityArgs args)
        {
            _gravity = _gravityData.GetValue();
            _jumpForce = Mathf.Sqrt(_jumpData.Height * -2 * _gravity);
        }

        protected override void OnStart(ref EntityArgs args)
        {
            _elapsedTime = 0f;
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;

            _elapsedTime += delta;
            float t = Mathf.Clamp01(_elapsedTime / _jumpData.Duration);
            float curveValue = _jumpData.JumpCurve.Evaluate(t);

            // Convert curve output into vertical speed
            _velocity = curveValue * Mathf.Sqrt(_jumpData.Height * -2 * _gravity);

            var input = args.Controller.MoveDirection() * _jumpData.MoveSpeed;
            var jumpDir = Vector3.up * _velocity;

            movement.AddMoveDir(input);
            movement.Move(jumpDir, delta);
        }

        protected override bool OnCompleted(ref EntityArgs args)
        {
            return _elapsedTime >= _jumpData.Duration;
        }
        
        protected override void OnDispose()
        {
            _jumpData = null;
            _gravityData = null;
        }
    }
}