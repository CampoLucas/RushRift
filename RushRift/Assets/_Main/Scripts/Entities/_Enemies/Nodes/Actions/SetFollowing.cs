using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class SetFollowing : ActionData
    {
        public bool Value => value;
        
        [SerializeField] private bool value;
        
        protected override INode OnCreateNode()
        {
            return new SetFollowingProxy(this);
        }
    }
    
    public class SetFollowingProxy : Node<SetFollowing>
    {
        private IController _controller;
        
        public SetFollowingProxy(SetFollowing data) : base(data)
        {
            
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override NodeState OnUpdate()
        {
            if (_controller.GetModel().TryGetComponent<EnemyComponent>(out var enemyComponent))
            {
                enemyComponent.SetFollowing(Data.Value);
            }
            
            return NodeState.Success;
        }

        protected override bool TryFailure(out string message)
        {
            if (_controller == null)
            {
                message = "WARNING: IsFollowing doesn't have a controller reference. Returning failure";
                return true;
            }

            return base.TryFailure(out message);
        }

        protected override void OnDispose()
        {
            _controller = null;
        }
    }
}