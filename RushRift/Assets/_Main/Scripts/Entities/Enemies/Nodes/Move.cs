using System;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public enum Direction
    {
        Forward, Right, MoveDir, DamageDirection
    }
    
    public class Move : ActionData
    {
        public Direction Direction => direction;
        public bool Invert => invert;

        [SerializeField] private Direction direction;
        [SerializeField] private bool invert;

        protected override INode OnCreateNode()
        {
            return new MoveProxy(this);
        }
    }

    public class MoveProxy : Node<Move>
    {
        private IController _controller;
        private IMovement _movement;
        
        public MoveProxy(Move data) : base(data)
        {
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override void OnStart()
        {
            if (_controller == null) return;

            if (_movement == null && _controller.GetModel().TryGetComponent<IMovement>(out var move))
            {
                _movement = move;
            }
        }

        protected override NodeState OnUpdate()
        {
            var dir = GetDirection();

            _movement.AddMoveDir(Data.Invert ? -dir : dir);
            return NodeState.Success;
        }

        protected override bool TryFailure(out string message)
        {
            if (_controller == null)
            {
                message = "WARNING: The FollowTarget doesn't have a controller reference. Returning failure";
                return true;
            }
            
            // if (_enemyComp == null)
            // {
            //     message = "WARNING: The FollowTarget's controller doesn't have a EnemyComponent. Returning failure";
            //     return true;
            // }
            
            if (_movement == null)
            {
                message = "WARNING: The FollowTarget's controller doesn't have a IMovement Component. Returning failure";
                return true;
            }
            
            return base.TryFailure(out message);
        }

        protected override void OnDispose()
        {
            _controller = null;
            _movement = null;
        }

        private Vector3 GetDirection()
        {
            switch (Data.Direction)
            {
                case Direction.Forward:
                    return _controller.Transform.forward;
                case Direction.Right:
                    return _controller.Transform.right;
                case Direction.MoveDir:
                    return _controller.MoveDirection();
                case Direction.DamageDirection:
                    if (_controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
                    {
                        return (_controller.Transform.position - healthComponent.DamagePosition).normalized;
                    }
                    return Vector3.zero;
                default:
                    return Vector3.zero;
            }
        }
    }
}