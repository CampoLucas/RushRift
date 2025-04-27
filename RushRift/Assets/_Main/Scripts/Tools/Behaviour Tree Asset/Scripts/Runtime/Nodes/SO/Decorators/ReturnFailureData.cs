using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class ReturnFailureData : DecoratorData
    {
        private class ReturnFailure : Node<ReturnFailureData>
        {
            public ReturnFailure(ReturnFailureData data) : base(data)
            {
            }
            
            protected override NodeState OnUpdate()
            {
                GetChild().DoUpdate();
                return NodeState.Failure;
            }
        }

        protected override INode OnCreateNode()
        {
            return new ReturnFailure(this);
        }
    }
}