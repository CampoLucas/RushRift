using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class IsFollowing : ConditionalData
    {
        protected override INode OnCreateNode()
        {
            return new IsFollowingProxy(this);
        }
    }
    
    public class IsFollowingProxy : Node<IsFollowing>
    {
        private IController _controller;
        
        public IsFollowingProxy(IsFollowing data) : base(data)
        {
            
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override void OnStart()
        {
            
        }

        protected override NodeState OnUpdate()
        {
            if (_controller.GetModel().TryGetComponent<EnemyComponent>(out var enemyComponent) && enemyComponent.IsFollowing())
            {
                return NodeState.Success;
            }
            
            return NodeState.Failure;
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