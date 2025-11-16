using System;
using Game.InputSystem;

namespace Game.Predicates
{
    
    
    public class InputButtonPredicate : IPredicate
    {
        public enum State { Hold, Down, Up, }

        private readonly InputManager.Input _input;
        private readonly State _state;

        public InputButtonPredicate(InputManager.Input input, State inputState)
        {
            _input = input;
            _state = inputState;
        }

        public bool Evaluate()
        {
            return GetInput();
        }

        public void Dispose()
        {
            
        }

        private bool GetInput()
        {
            switch (_state)
            {
                case State.Hold:
                    return InputManager.OnButton(_input);
                case State.Down:
                    return InputManager.OnButtonDown(_input);
                case State.Up:
                    return InputManager.OnButtonUp(_input);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}