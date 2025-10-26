using Game.Entities;
using Game.InputSystem;

namespace Game.Predicates
{
    public class CompareStatePredicate : IPredicate<EntityArgs>
    {
        private readonly HashedKey _state;
        private readonly bool _value;

        public CompareStatePredicate(HashedKey state, bool value = true)
        {
            _state = state;
            _value = value;
        }
        
        public bool Evaluate(ref EntityArgs args)
        {
            var result = args.StateMachine.Current == _state;
            return result == _value;
        }

        public void Dispose()
        {
            
        }
    }
}