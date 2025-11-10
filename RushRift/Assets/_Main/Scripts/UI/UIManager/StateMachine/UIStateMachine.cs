using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.UI.StateMachine.Interfaces;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public class UIStateMachine : IDisposable
    {
        public UIScreen Current { get; private set; }
        
        private UIState _current;
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
                state.Init();
                state.Disable();
                return true;
            }

            state?.Dispose();
            return false;
        }

        public bool TryAddState(UIScreen screen, Func<UIState> state)
        {
            if (_states.ContainsKey(screen))
            {
                return false;
            }

            var s = state();

            if (s == null || !_states.TryAdd(screen, s)) return false;
            
            _statesList.Add(screen);
            s.Init();
            s.Disable();
            return true;

        }

        public bool TryAddState(UIScreen screen, BaseUIPresenter presenter, out UIState state)
        {
            if (_states.ContainsKey(screen) || !presenter.TryGetState(out state) || state == null || !_states.TryAdd(screen, state))
            {
                state = null;
                return false;
            }
            
            _statesList.Add(screen);
            state.Init();
            state.Disable();
            return true;
        }

        public bool TransitionTo(UIScreen to, float fadeOut, float fadeIn, float fadeInStartTime)
        {
            if (!_states.TryGetValue(to, out var state) || _current == state) return false;

            _timer = 0;
            _effectTransition.Set(new UIEffectTransition(_current, state, fadeOut, 0, fadeIn, fadeInStartTime));
            
            Current = to;
            _current = state;
            return true;
        }

        public void Update(float delta)
        {
            if (_effectTransition.TryGet(out var effectTransition))
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

        public bool TrySetTransition(UIScreen from, UIScreen to, IPredicate predicate)
        {
            if (_states.ContainsKey(to) && _states.TryGetValue(from, out var state) && predicate != null)
            {
                state.AddTransition(to, predicate);
                return true;
            }

            predicate?.Dispose();
            return false;
        }
    } 
}