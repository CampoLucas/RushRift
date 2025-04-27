using System.Collections;
using System.Collections.Generic;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime
{
    

    public sealed class RootData : NodeData
    {
        private sealed class Root : Node<RootData>
        {
            public Root(RootData data) : base(data)
            {
            }

            protected override NodeState OnUpdate()
            {
                return GetChild().DoUpdate();
            }
        }
        public override int ChildCapacity() => 1;
        public override bool IsRoot() => true;

        protected override INode OnCreateNode()
        {
            return new Root(this);
        }
    }
}
