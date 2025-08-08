using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.AttackSystem;

namespace Game.BehaviourTree.Nodes
{
    public class Attack : ActionData
    {
        protected override INode OnCreateNode()
        {
            return new AttackProxy(this);
        }
    }

    public class AttackProxy : Node<Attack>
    {
        private IController _controller;
        public AttackProxy(Attack data) : base(data)
        {
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override void OnStart()
        {
            if (_controller.GetModel().TryGetComponent<ComboHandler>(out var combo))
            {
                combo.ForceAttack();
            }
        }

        protected override NodeState OnUpdate()
        {
            if (_controller.GetModel().TryGetComponent<ComboHandler>(out var combo) && combo.Attacking())
            {
                return NodeState.Running;
            }

            return NodeState.Success;
        }
    }
}