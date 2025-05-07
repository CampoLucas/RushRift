using Game.Entities;

namespace Game.Predicates
{
    public class IsMovingPredicate : IPredicate<EntityArgs>
    {
        private readonly bool _value;

        public IsMovingPredicate(bool value = true)
        {
            _value = value;
        }
        
        public bool Evaluate(ref EntityArgs args)
        {
            var isMoving = args.Controller.MoveDirection().magnitude > 0.1f;
            return isMoving == _value;
        }

        public void Dispose()
        {
            
        }
    }
}