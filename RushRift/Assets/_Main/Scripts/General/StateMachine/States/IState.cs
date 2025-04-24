using System;
using System.Collections.Generic;
using Game.Inputs;

namespace Game
{
    public interface IState<TArgs> : IDisposable where TArgs : IDisposable
    {
        void Init(ref TArgs args);
        void StartState(ref TArgs args);
        void UpdateState(ref TArgs args, float delta);
        void ExitState(ref TArgs args);
        
        bool AddTransition(HashedKey to, IPredicate<TArgs> condition);
        bool AddTransition<TTransition>(HashedKey to, ITransition<TArgs> transition) where TTransition : ITransition<TArgs>;
        bool TryGetTransition(IStateMachine<TArgs> stateMachine, ref TArgs args, out ITransition<TArgs> transition);
        bool Completed(ref TArgs args);
    }
}