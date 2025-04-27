using System;
using System.Collections.Generic;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using UnityEngine;

namespace BehaviourTreeAsset.Interfaces
{
    public interface IBehaviour : IDisposable
    {
        INode Root { get; }
        NodeState CurrentState { get; }
        GameObject Owner { get; }
        List<INode> Nodes { get; }
        BehaviourTreeRunner Runner { get; }

        void DoAwake(GameObject owner);
        void Reset();
        NodeState DoUpdate();
        void SetRunner(BehaviourTreeRunner runner);
    }
}