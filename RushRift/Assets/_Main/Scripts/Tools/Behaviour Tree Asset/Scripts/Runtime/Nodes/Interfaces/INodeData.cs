using System;
using System.Collections.Generic;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Interfaces
{
    public interface INodeData
    {
        bool Enabled { get; }
        string Name { get; }
        string Description { get; }
        Vector2 Position { get; }
        List<NodeData> Children { get; }

        void Init(BehaviourTreeData behaviourTreeData);
        void Destroy();
        
        int GetChildCount();
        int ChildCapacity();
        bool AddChild(NodeData nodeData);
        bool RemoveChild(NodeData nodeData);
        bool ContainsChild(NodeData nodeData);
        bool ContainsChildInChildren(NodeData nodeData);
        INode CreateNode();
        bool TryCreateNode(out INode node);
        bool IsRoot();
    }
    
    /*
     * Node Ideas:
     * SubBehaviour : Action, IBehaviourRunner
     * NodeReference : Action
     * Resetable : Decorator
     * DoOnce : Resetable
     * DoNTimes : Resetable
     * ReturnFailure : Decorator
     * ReturnSuccess : Decorator
     */
}