using System;

namespace Game.InputSystem
{
    public class OnButtonPredicate : IPredicate
    {
        private readonly InputManager.Input _input;
        private readonly ButtonAction _action;

        public OnButtonPredicate(InputManager.Input input, ButtonAction action = ButtonAction.Down)
        {
            _input = input;
            _action = action;
        }
        
        public bool Evaluate()
        {
            switch(_action)
            {
                case ButtonAction.Get:
                    return InputManager.OnButton(_input);
                case ButtonAction.Down:
                    return InputManager.OnButtonDown(_input);
                case ButtonAction.Up:
                    return InputManager.OnButtonUp(_input);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void Dispose()
        {
            
        }
    }

    public enum ButtonAction
    {
        Get, Down, Up
    }
}