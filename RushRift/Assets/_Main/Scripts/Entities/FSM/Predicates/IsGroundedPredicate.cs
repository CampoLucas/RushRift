using Game.Entities.Components;
using Game.Entities.States;

namespace Game.Predicates
{
    public class IsGroundedPredicate : IPredicate<EntityArgs>
    {
        private readonly bool _value;

        public IsGroundedPredicate(bool value = true)
        {
            _value = value;
        }

        public bool Evaluate(ref EntityArgs args)
        {
            if (!args.Controller.GetModel().TryGetComponent<IMovement>(out var movement)) return false;
            
            var isGrounded = movement.Grounded;
            return isGrounded == _value;
        }

        public void Dispose()
        {
            
        }
    }
}