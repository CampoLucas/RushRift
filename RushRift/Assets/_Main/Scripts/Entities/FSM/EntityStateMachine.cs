using Game.Inputs;


namespace Game.Entities
{
    public class EntityStateMachine : StateMachine<EntityArgs>
    {
        public EntityStateMachine(HashedKey rootKey, IState<EntityArgs> rootState, IController controller) : base(rootKey, rootState)
        {
            Args = new EntityArgs(this, controller);
        }

        public EntityStateMachine(IController controller)
        {
            Args = new EntityArgs(this, controller);
        }
    }
}