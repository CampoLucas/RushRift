using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.UI.Screens
{
    public class UIStateMachine : IDisposable
    {
        private UIState _current;
        private Dictionary<UIScreen, UIState> _states = new();
        private NullCheck<UITransition> _transition; // ToDo: Any transition
        private float _timer;


        public bool TryAddState(UIScreen screen, UIState state) => state != null && _states.TryAdd(screen, state);

        public bool TryChangeState(UIScreen screen)
        {
            if (!_states.TryGetValue(screen, out var state)) return false;
            
            if (_current != null) _current.Disable();

            _current = state;
            _current.Enable();
            return true;
        }

        public bool TransitionTo(UIScreen to, float fadeOut, float fadeIn, float fadeInStartTime)
        {
            if (!_states.TryGetValue(to, out var state)) return false;

            _transition.Set(new UITransition(_current, state, fadeOut, 0, fadeIn, fadeInStartTime));
            _timer = 0;
            _current = state;
            return true;
        }

        public void Update(float delta)
        {
            if (_transition.TryGetValue(out var transition))
            {
                _timer += delta;
                
                if (transition.DoTransition(_timer)) _transition.Set(null);
            }
        }

        public void Dispose()
        {
            
        }
    } 
}