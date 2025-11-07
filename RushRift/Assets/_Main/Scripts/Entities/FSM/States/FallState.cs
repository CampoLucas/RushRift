using Game.Entities.Components;
using UnityEngine;
using Game.Utils;

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
        
        // protected override void OnInit(ref EntityArgs args)
        // {
        //     return;
        //     //_gravity = _gravityData.GetValue();
        // }

        // protected override void OnStart(ref EntityArgs args)
        // {
        //     return;
        //     if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
        //     movement.EnableGravity(false);
        //     _velocity = movement.Velocity.y;
        //     if (_velocity > 0) _velocity = 0;
        // }

        // protected override void OnUpdate(ref EntityArgs args, float delta)
        // {
        //     return;
        //     if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
        //
        //     _velocity += _gravity * delta;
        //
        //     var input = args.Controller.MoveDirection();
        //     var controlledInput = Vector3.Lerp(Vector3.zero, input, _gravityData.FallAirControl).XOZ();
        //     var jumpDir = Vector3.up * _velocity;
        //     
        //     // Instead of separating input and vertical, COMBINE them
        //     //Vector3 finalMove = controlledInput * _gravityData.AirAcceleration + new Vector3(0, _velocity, 0);
        //
        //     movement.AddMoveDir(controlledInput);
        //     movement.Move(jumpDir, delta);
        //     //movement.AddMoveDir(finalMove);
        //     //movement.Move(finalMove, delta);
        // }

        // protected override void OnExit(ref EntityArgs args)
        // {
        //     return;
        //     if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return;
        //     movement.EnableGravity(true);
        // }
    }
}