using System;
using Game.Inputs;

namespace Game
{
    public interface IStateMachine<TArgs> : IDisposable
        where TArgs : IDisposable
    {
        HashedKey Current { get; }
        NullCheck<IState<TArgs>> CurrentState { get; }

        void Run(float delta);
        bool SetState(HashedKey key);
        bool AddState(HashedKey key, IState<TArgs> state);
        void SetRootState(HashedKey key);
        bool TryGetState(HashedKey key, out IState<TArgs> state);
        bool TryGetState<TState>(HashedKey key, out TState state) where TState : IState<TArgs>;
        bool RemoveState(HashedKey key);
        bool AddAnyTransition(HashedKey toKey, IPredicate<TArgs> condition);
        bool AddAnyTransition<TStateMachine>(HashedKey toKey, ITransition<TArgs> transition) where TStateMachine : IStateMachine<TArgs>;
    }
}