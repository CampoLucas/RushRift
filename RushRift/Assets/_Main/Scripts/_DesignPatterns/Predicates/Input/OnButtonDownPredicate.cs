using Game.Inputs;

namespace Game.Input
{
    public class OnButtonDownPredicate : IPredicate
    {
        private HashedKey _input;

        public OnButtonDownPredicate(HashedKey input)
        {
            _input = input;
        }
        
        public bool Evaluate()
        {
            return InputManager.OnButtonDown(_input);
        }
        
        public void Dispose()
        {
            
        }
    }
}