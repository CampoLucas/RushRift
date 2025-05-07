using System.Collections.Generic;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BehaviourTreeAsset.Runtime
{
    public class BehaviourTree : IBehaviour
    {
        public INode Root { get; private set; }
        public NodeState CurrentState { get; private set; } = NodeState.Running;
        public GameObject Owner { get; private set; }
        public List<INode> Nodes { get; private set; } = new();
        public BehaviourTreeRunner Runner { get; private set; }

        public BehaviourTree(IBehaviourData data, GameObject owner)
        {
            Owner = owner;
            SetRoot(data.Root.CreateNode());
            
        }

        private void SetRoot(INode node)
        {
            Root = node;
        }
        
        public void DoAwake(GameObject owner)
        {
            if (owner == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"BehaviourTreeAsset: The Owner GameObject in the DoAwake is null.");
#endif
                return;          
            }
            
            Owner = owner;
            Root.DoAwake(Owner, this);
        }

        public void Reset()
        {
            Root.Reset();
            CurrentState = NodeState.Running;
        }

        public NodeState DoUpdate()
        {
            if (Root.CurrentState == NodeState.Running)
            {
                CurrentState = Root.DoUpdate();
            }

            return CurrentState;
        }

        public void SetRunner(BehaviourTreeRunner runner)
        {
            Runner = runner;
        }

        public void Dispose()
        {
            if (Root != null) Root.Dispose();
            Owner = null;
        }
    }
}
