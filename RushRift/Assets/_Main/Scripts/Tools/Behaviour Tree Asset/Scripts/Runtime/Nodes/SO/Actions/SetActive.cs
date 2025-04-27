using System.Collections;
using System.Collections.Generic;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class SetActive : ActionData
    {
        public bool Value => value;
        
        [SerializeField] private bool value;

        protected override INode OnCreateNode()
        {
            return new SetActiveProxy(this);
        }
    }

    public class SetActiveProxy : Node<SetActive>
    {
        public SetActiveProxy(SetActive data) : base(data)
        {
        }

        protected override NodeState OnUpdate()
        {
            Owner.SetActive(Data.Value);
            return NodeState.Success;
        }
    }
}
