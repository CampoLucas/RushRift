using System;
using Game.Inputs;

namespace Game
{
    public interface ITransition<TArgs> : IDisposable 
        where TArgs : IDisposable
    {
        void Do(IStateMachine<TArgs> stateMachine, ref TArgs args);
        bool Evaluate(IStateMachine<TArgs> stateMachine, ref TArgs args);
        void SetTransition(HashedKey to);
    }
}