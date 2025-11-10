using System;
using Game.InputSystem;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public class UIOnButtonPredicate : UIPredicate
    {
        public HashedKey Input => new HashedKey(input);
        
        [SerializeField] private string input;
        [SerializeField] private ButtonAction action;


        protected override bool OnEvaluate()
        {
            switch(action)
            {
                case ButtonAction.Get:
                    return InputManager.OnButton(Input);
                case ButtonAction.Down:
                    return InputManager.OnButtonDown(Input);
                case ButtonAction.Up:
                    return InputManager.OnButtonUp(Input);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}