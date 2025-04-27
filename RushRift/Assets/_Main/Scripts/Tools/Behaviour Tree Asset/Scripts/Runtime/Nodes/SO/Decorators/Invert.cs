using System;
using System.Collections;
using System.Collections.Generic;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class Invert : DecoratorData
    {
        protected override INode OnCreateNode()
        {
            return new InvertProxy(this);
        }
    }

    public class InvertProxy : Node<Invert>
    {
        public InvertProxy(Invert data) : base(data)
        {
        }

        protected override NodeState OnUpdate()
        {
            var child = GetChild().DoUpdate();
            switch (child)
            {
                case NodeState.Running:
                    return NodeState.Running;
                case NodeState.Failure:
                    return NodeState.Success;
                case NodeState.Success:
                    return NodeState.Failure;
                case NodeState.Disable:
                    return NodeState.Disable;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
