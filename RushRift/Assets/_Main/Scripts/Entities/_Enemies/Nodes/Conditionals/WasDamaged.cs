using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Components;

namespace Game.BehaviourTree.Nodes
{
    public class WasDamaged : ConditionalData
    {
        protected override INode OnCreateNode()
        {
            return new WasDamagedProxy(this);
        }
    }

    public class WasDamagedProxy : Node<WasDamaged>
    {
        private IController _controller;
        
        public WasDamagedProxy(WasDamaged data) : base(data)
        {
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override NodeState OnUpdate()
        {
            if (_controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent) &&
                healthComponent.Damaged())
            {
                return NodeState.Success;
            }

            return NodeState.Failure;
        }

        protected override bool TryFailure(out string message)
        {
            if (_controller == null)
            {
                message = "WARNING: _controller in WasDamaged is null. Returning Failure";
                return false;
            }
            
            return base.TryFailure(out message);
        }

        protected override void OnDispose()
        {
            _controller = null;
        }
    }
}