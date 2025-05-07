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
        private Dictionary<Type, UIState> _states = new();
        private NullCheck<UITransition> _transition; // ToDo: Any transition
        private float _timer;

        
        public bool TryAddState<T>(T state) where T : UIState => state != null && _states.TryAdd(typeof(T), state);

        public bool TryChangeState<T>() where T : UIState
        {
            var type = typeof(T);
            if (!_states.TryGetValue(type, out var state)) return false;
            
            if (_current != null) _current.Disable();

            _current = state;
            _current.Enable();
            return true;
        }

        public bool TransitionTo<T>(float fadeOut, float fadeIn, float fadeInStartTime) where T : UIState
        {
            var type = typeof(T);
            if (!_states.TryGetValue(type, out var state)) return false;

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