using System;
using Game.Inputs;
using UnityEngine.UI;

namespace Game.Input
{
    public class OnButtonPredicate : IPredicate
    {
        private readonly HashedKey _input;
        private readonly ButtonAction _action;

        public OnButtonPredicate(HashedKey input, ButtonAction action = ButtonAction.Down)
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