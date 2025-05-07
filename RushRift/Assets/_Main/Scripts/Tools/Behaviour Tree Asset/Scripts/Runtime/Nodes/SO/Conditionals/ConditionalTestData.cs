using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class ConditionalTestData : CompositeData
    {
        private class ConditionalTest : Node<ConditionalTestData>
        {
            public ConditionalTest(ConditionalTestData data) : base(data)
            {
            }
            
            protected override NodeState OnUpdate()
            {
                return Data.numDifferentThanZero != 0 ? NodeState.Success : NodeState.Failure;
            }
        }
        
        public int numDifferentThanZero;

        protected override INode OnCreateNode()
        {
            return new ConditionalTest(this);
        }
    }
}