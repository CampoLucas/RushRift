using Game.Entities;
using Game.Inputs;

namespace Game.Predicates
{
    public class StateCompletedPredicate : IPredicate<EntityArgs>
    {
        public bool Evaluate(ref EntityArgs args)
        {
            var stateMachine = args.StateMachine;
            return stateMachine != null && stateMachine.CurrentState.TryGet(out var state) && state.Completed(ref args);
        }

        public void Dispose()
        {
            
        }
    }
}