using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class FallState : State<EntityArgs>
    {
        private GravityData _gravityData;

        private float _gravity;
        private float _velocity;

        public FallState(GravityData gravityData)
        {
            _gravityData = gravityData;
        }
        
        protected override void OnInit(ref EntityArgs args)
        {
            _gravity = _gravityData.GetValue();
        }

        protected override void OnStart(ref EntityArgs args)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
            _velocity = movement.Velocity.y;
        }

        protected override void OnUpdate(ref EntityArgs args, float delta)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
            _velocity += _gravity * delta;
            var input = args.Controller.MoveDirection();
            var dir = new Vector3(0, _velocity, 0);
            movement.AddMoveDir(input);
            movement.Move(dir, delta);
        }
    }
}