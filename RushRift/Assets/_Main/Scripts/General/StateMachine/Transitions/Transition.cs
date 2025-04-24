using System;
using Game.Inputs;

namespace Game
{
    public class Transition<T> : ITransition<T> 
        where T : IDisposable
    {
        public HashedKey To { get; private set; }
        
        private IPredicate<T> _condition;

        public Transition(HashedKey to, IPredicate<T> condition)
        {
            To = to;
            _condition = condition;
        }

        public void Do(IStateMachine<T> stateMachine, ref T args)
        {
            stateMachine.SetState(To);
        }

        public bool Evaluate(IStateMachine<T> stateMachine, ref T args)
        {
            return _condition?.Evaluate(ref args) ?? false;
        }

        public void SetTransition(HashedKey to)
        {
            To = to;
        }
        
        public void Dispose()
        {
            _condition?.Dispose();
            _condition = null;
        }
    }
}