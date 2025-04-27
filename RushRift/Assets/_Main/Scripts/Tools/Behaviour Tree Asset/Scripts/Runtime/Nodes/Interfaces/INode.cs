using System;
using System.Collections.Generic;
using BehaviourTreeAsset.Interfaces;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Interfaces
{
    public interface INode : IDisposable
    {
        NodeState CurrentState { get; }
        List<INode> Children { get; }

        void DoAwake(GameObject owner, IBehaviour ownerBehaviour);
        NodeState DoUpdate();

        INode GetChild();
        void SetChildren(List<INode> newChildren);
        void SetChildren(INodeData data);
        int GetChildCount();
        int ChildCapacity();
        INode GetChild(int index);
        bool TryGetChild(int index, out INode child);
        bool IsRoot();

        void SetBehaviour(IBehaviour behaviour);
        void Reset();
    }
}