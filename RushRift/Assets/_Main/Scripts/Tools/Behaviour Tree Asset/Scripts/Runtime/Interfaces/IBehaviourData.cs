using System.Collections.Generic;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Interfaces
{
    public interface IBehaviourData
    {
        NodeData Root { get; }
        List<NodeData> Nodes { get; }
        
        IBehaviour CreateBehaviour(GameObject owner, BehaviourTreeRunner runner);
        void OnPopulateView();
    }
}