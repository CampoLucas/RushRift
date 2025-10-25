using System;
using System.Collections.Generic;
using Game.InputSystem;

namespace Game
{
    public class State<T> : IState<T> where T : IDisposable
    {
        private HashSet<ITransition<T>> _transitions = new();

        #region Public Methods

        public void Init(ref T args)
        {
            OnInit(ref args);
        }

        public void StartState(ref T args)
        {
            OnStart(ref args);
        }

        public void UpdateState(ref T args, float delta)
        {
            OnUpdate(ref args, delta);
        }

        public void ExitState(ref T args)
        {
            OnExit(ref args);
        }
        
        public bool AddTransition(HashedKey to, IPredicate<T> condition)
        {
            if (condition == null) return false;
            
            var tr = new Transition<T>(to, condition);
            _transitions.Add(tr);
            return true;
        }
        
        public bool AddTransition<TTransition>(HashedKey to, ITransition<T> transition)
            where TTransition : ITransition<T>
        {
            if (transition == null || !_transitions.Add(transition)) return false;
            return true;
        }

        public bool TryGetTransition(IStateMachine<T> stateMachine, ref T args, out ITransition<T> transition)
        {
            return StateMachine<T>.TryGetTransition(_transitions, stateMachine, ref args, out transition);
        }

        public bool Completed(ref T args)
        {
            return OnCompleted(ref args);
        }

        public void Dispose()
        {
            OnDispose();
            ClearTransitions();
            _transitions = null;
        }

        #endregion

        #region Protected Methods

        protected virtual void OnInit(ref T args) { }
        protected virtual void OnStart(ref T args) { }
        protected virtual void OnUpdate(ref T args, float delta) { }
        protected virtual void OnExit(ref T args) { }
        protected virtual bool OnCompleted(ref T args) => true;
        protected virtual void OnDispose() { }

        #endregion

        #region Private Methods

        private void ClearTransitions()
        {
            foreach (var tr in _transitions)
            {
                if (tr == null) continue;
                tr.Dispose();
            }
            
            _transitions.Clear();
        }

        #endregion
    }
}