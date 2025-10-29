using System;
using System.Collections.Generic;
using Game.Entities;
using Game.InputSystem;
using UnityEngine;

namespace Game
{
    public class StateMachine<TArgs> : IStateMachine<TArgs>
        where TArgs : IDisposable
    {
        public HashedKey Current => _current;
        public NullCheck<IState<TArgs>> CurrentState => _currState;
        
        protected TArgs Args;
        
        private HashedKey _rootState;
        private HashedKey _current;
        private NullCheck<IState<TArgs>> _currState;
        
        private HashSet<HashedKey> _hashesList = new();
        private Dictionary<HashedKey, IState<TArgs>> _statesDict = new();
        private HashSet<ITransition<TArgs>> _anyTransitions = new();

        public StateMachine() { }
        
        public StateMachine(HashedKey rootKey, IState<TArgs> rootState)
        {
            if (AddState(rootKey, rootState))
            {
                SetRootState(rootKey);
                SetState(rootKey);
            }
        }

        public void Run(float delta)
        {
            if (!_currState)
            {
#if UNITY_EDITOR
                Debug.LogError("[STATE_MACHINE_ERROR] The current state is null.");
#endif
                
                return;
            }

            if (TryGetTransition(out var newState))
            {
                newState.Do(this, ref Args);
            }
            
            _currState.Get().UpdateState(ref Args, delta);
        }

        public bool SetState(HashedKey key)
        {
            if (Current == key || !_statesDict.TryGetValue(key, out var state)) return false;
            
            if (_currState) _currState.Get().ExitState(ref Args);
            _currState.Set(state);
            _current = key;
            _currState.Get().StartState(ref Args);
            return true;
        }

        public bool AddState(HashedKey key, IState<TArgs> state)
        {
            if (_statesDict.ContainsKey(key) || state == null) return false;
            _statesDict[key] = state;
            _hashesList.Add(key);
            state.Init(ref Args);
            return true;
        }

        public void SetRootState(HashedKey rootKey)
        {
            _rootState = rootKey;
        }

        public bool TryGetState(HashedKey key, out IState<TArgs> state)
        {
            return _statesDict.TryGetValue(key, out state);
        }

        public bool TryGetState<TState>(HashedKey key, out TState state)
            where TState : IState<TArgs>
        {
            state = default;
            if (_statesDict.TryGetValue(key, out var s) && s is TState castedState)
            {
                state = castedState;
                return true;
            }

            return false;
        }

        public bool RemoveState(HashedKey key)
        {
            if (!_statesDict.ContainsKey(key)) return false;
            _statesDict.Remove(key);
            _hashesList.Remove(key);
            return true;
        }

        public bool AddAnyTransition(HashedKey toKey, IPredicate<TArgs> condition)
        {
            if (condition == null) return false;
            
            var tr = new Transition<TArgs>(toKey, condition);
            _anyTransitions.Add(tr);
            return true;
        }

        public bool AddAnyTransition<TStateMachine>(HashedKey toKey, ITransition<TArgs> transition) 
            where TStateMachine : IStateMachine<TArgs>
        {
            if (transition == null || !_anyTransitions.Add(transition)) return false;
            return true;
        }

        private void ClearStates()
        {
            if (_hashesList == null) Debug.LogError("the hashed list is null");
            foreach (var hashKey in _hashesList)
            {
                var state = _statesDict[hashKey];
                if (state == null) continue;
                state.Dispose();
            }
            
            _hashesList.Clear();
            _statesDict.Clear();
        }

        private void ClearAnyTransitions()
        {
            foreach (var tr in _anyTransitions)
            {
                if (tr == null) continue;
                tr.Dispose();
            }
            
            _anyTransitions.Clear();
        }

        public static bool TryGetTransition<TTransition>(ICollection<TTransition> transitions, IStateMachine<TArgs> stateMachine, ref TArgs args, out ITransition<TArgs> transition)
            where TTransition : ITransition<TArgs>
        {
            transition = default;
            
            if (transitions == null) return false;
            var result = false;

            foreach (var tr in transitions)
            {
                if (tr == null || !tr.Evaluate(stateMachine, ref args)) continue;
                transition = tr;
                result = true;
                break;
            }

            return result;
        }
        
        private bool TryGetTransition(out ITransition<TArgs> transition)
        {
            if (TryGetTransition<ITransition<TArgs>>(_anyTransitions, this, ref Args, out transition) || 
                (_currState && _currState.Get().TryGetTransition(this , ref Args, out transition))) return true;
            return false;
        }
        
        public void Dispose()
        {
            ClearStates();
            _hashesList = null;
            _statesDict = null;
            
            ClearAnyTransitions();
            _anyTransitions = null;
            
            Args.Dispose();
        }
    }
}
