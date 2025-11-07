using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class MoveState : State<EntityArgs>
    {
        private readonly MoveType _moveType;
        private NullCheck<IMovement> _movement;
        
        public MoveState(MoveType moveType)
        {
            _moveType = moveType;
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

            if (_movement)
            {
                _movement.Get().SetProfile(_moveType);
            }
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            var controller = args.Controller;
            if (!_movement)
            {
                return;
            }
            _movement.Get().AddMoveDir(controller.MoveDirection());
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _movement.Dispose();
        }
    }
}