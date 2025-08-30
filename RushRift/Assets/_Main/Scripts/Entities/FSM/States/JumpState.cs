using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class JumpState : State<EntityArgs>
    {
        private readonly MoveType _moveType;
        private JumpData _data;
        private NullCheck<IMovement> _movement;
        
        private float _gravity;
        private float _velocity;
        private float _elapsedTime;
        

        public JumpState(JumpData data, MoveType moveType)
        {
            _moveType = moveType;
            _data = data;
        }

        protected override void OnStart(ref EntityArgs args)
        {
            if (!_movement)
            {
                if (args.Controller.GetModel().TryGetComponent<IMovement>(out var movement))
                {
                    _movement.Set(movement);
                }
            }

            if (_movement.TryGetValue(out var m))
            {
                m.SetProfile(_moveType);
                m.EnableGravity(false);
            }
            
            _elapsedTime = 0f;
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            if (!_movement.TryGetValue(out var movement)) return;

            movement.AddMoveDir(args.Controller.MoveDirection());
            
            _elapsedTime += delta;
            var t = Mathf.Clamp01(_elapsedTime / _data.Duration);
            var curve = _data.Curve.Evaluate(t);

            _velocity = curve * _data.Force;

            movement.SetYVelocity(_velocity);
        }

        protected override void OnExit(ref EntityArgs args)
        {
            if (!_movement) return;
            _movement.Get().EnableGravity(true);
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