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
        private UIScreen _currentScreen;
        private Dictionary<UIScreen, UIState> _states = new();
        private List<UIScreen> _statesList = new();
        private NullCheck<UIEffectTransition> _effectTransition; // ToDo: Any transition

        private HashSet<UITransition> _fromAny = new();
        
        private float _timer;


        public bool TryAddState(UIScreen screen, UIState state)
        {
            if (state != null && _states.TryAdd(screen, state))
            {
                _statesList.Add(screen);
                state.Disable();
                return true;
            }

            state?.Dispose();
            return false;
        }

        public bool TryChangeState(UIScreen screen)
        {
            if (!_states.TryGetValue(screen, out var state) || _current == state) return false;
            
            if (_current != null) _current.Disable();
            
            _current = state;
            _current.Enable();
            return true;
        }

        public bool TransitionTo(UIScreen to, float fadeOut, float fadeIn, float fadeInStartTime)
        {
            if (!_states.TryGetValue(to, out var state) || _current == state) return false;

            _timer = 0;
            _effectTransition.Set(new UIEffectTransition(_current, state, fadeOut, 0, fadeIn, fadeInStartTime));
            _current = state;
            return true;
        }

        public void Update(float delta)
        {
            if (_effectTransition.TryGetValue(out var effectTransition))
            {

                if (effectTransition.DoTransition(_timer))
                {
                    _effectTransition.Set(null);
                }
                _timer += delta;
            }
            else if (TryGetTransition(out var transition))
            {
                transition.Do(this);
            }
        }

        public void Dispose()
        {
            _current?.Dispose();
            _current = null;

            for (var i = 0; i < _statesList.Count; i++)
            {
                var key = _statesList[i];
                if (_states.TryGetValue(key, out var state))
                {
                    state.Dispose();
                }
            }
            
            _statesList.Clear();
            _states.Clear();

            foreach (var any in _fromAny)
            {
                any.Dispose();
            }
        }
        
        public static bool TryGetTransition(HashSet<UITransition> transitions, out UITransition transition)
        {
            transition = default;
            
            if (transitions == null) return false;
            var result = false;

            foreach (var tr in transitions)
            {
                if (tr == null || !tr.Evaluate()) continue;
                transition = tr;
                result = true;
                break;
            }

            return result;
        }

        private bool TryGetTransition(out UITransition transition)
        {
            if (TryGetTransition(_fromAny, out transition) || TryGetTransition(_current.Transitions, out transition))
            {
                return true;
            }

            return false;
        }
    } 
}