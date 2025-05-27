using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class JumpState : State<EntityArgs>
    {
        private JumpData _data;

        private float _gravity;
        private float _velocity;
        private float _elapsedTime;
        

        public JumpState(JumpData data)
        {
            _data = data;
        }

        protected override void OnStart(ref EntityArgs args)
        {
            _elapsedTime = 0f;
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
            movement.EnableGravity(false);
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;

            movement.AddMoveDir(args.Controller.MoveDirection());
            
            _elapsedTime += delta;
            var t = Mathf.Clamp01(_elapsedTime / _data.Duration);
            var curve = _data.Curve.Evaluate(t);

            _velocity = curve * _data.Force;

            movement.SetYVelocity(_velocity);
        }

        protected override void OnExit(ref EntityArgs args)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
            movement.EnableGravity(true);
        }

        protected override bool OnCompleted(ref EntityArgs args)
        {
            //return false;
            //return _elapsedTime >= _jumpData.Duration;
            return _velocity <= 0 || _elapsedTime >= _data.Duration;
        }
        
        protected override void OnDispose()
        {
            _data = null;
        }
    }
}