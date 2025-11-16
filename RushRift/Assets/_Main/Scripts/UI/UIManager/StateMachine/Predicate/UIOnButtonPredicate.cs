using System;
using Game.InputSystem;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public class UIOnButtonPredicate : UIPredicate
    {
        [SerializeField] private InputManager.Input input;
        [SerializeField] private ButtonAction action;


        protected override bool OnEvaluate()
        {
            switch(action)
            {
                case ButtonAction.Get:
                    return InputManager.OnButton(input);
                case ButtonAction.Down:
                    return InputManager.OnButtonDown(input);
                case ButtonAction.Up:
                    return InputManager.OnButtonUp(input);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}