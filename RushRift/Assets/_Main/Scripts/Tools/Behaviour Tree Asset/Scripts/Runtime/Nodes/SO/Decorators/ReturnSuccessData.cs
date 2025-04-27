using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class ReturnSuccessData : DecoratorData
    {
        private class ReturnSuccess : Node<ReturnSuccessData>
        {
            public ReturnSuccess(ReturnSuccessData data) : base(data)
            {
            }
            
            protected override NodeState OnUpdate()
            {
                GetChild().DoUpdate();
                return NodeState.Success;
            }
        }

        protected override INode OnCreateNode()
        {
            return new ReturnSuccess(this);
        }
    }
}