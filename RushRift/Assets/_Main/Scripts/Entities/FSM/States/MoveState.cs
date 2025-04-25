using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class MoveState : State<EntityArgs>
    {
        private MovementData _data;

        public MoveState(MovementData data)
        {
            _data = data;
        }

        protected override void OnStart(ref EntityArgs args)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
            movement.SetData(_data);
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            var controller = args.Controller;
            if (!controller.GetModel().TryGetComponent<IMovement>(out var movement))
            {
                return;
            }
            movement.AddMoveDir(controller.MoveDirection());
        }

        protected override void OnDispose()
        {
            _data = null;
        }
    }
}