using System;

namespace Game.Entities.States
{
    public struct EntityArgs : IDisposable
    {
        public EntityStateMachine StateMachine { get; private set; }
        public IController Controller { get; private set; }

        public EntityArgs(EntityStateMachine stateMachine, IController controller) : this()
        {
            StateMachine = stateMachine;
            Controller = controller;
        }
        
        public void Dispose()
        {
            StateMachine = null;
            Controller = null;
        }
    }
}