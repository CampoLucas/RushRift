using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class RepeatData : DecoratorData
    {
        private class Repeat : Node<RepeatData>
        {
            private int _count;
            private NodeState _prevNodeState;
            
            public Repeat(RepeatData data) : base(data)
            {
            }
            
            protected override void OnStart()
            {
                _count = 0;
                _prevNodeState = NodeState.Success;
            }

            protected override NodeState OnUpdate()
            {
                //if (!infinite && amount <= 0) return State.Success;
                if (!Data.infinite && _prevNodeState == NodeState.Success)
                {
                    if (_count >= Data.amount) return NodeState.Success;
                    _count++;
                }
            
                _prevNodeState = GetChild().DoUpdate();

                if (Data.stopOnFailure && _prevNodeState == NodeState.Failure) 
                    return NodeState.Failure;
            
                return NodeState.Running;
            }
            
        }
        
        public int amount = 1;
        public bool infinite;
        public bool stopOnFailure;

        protected override INode OnCreateNode()
        {
            return new Repeat(this);
        }
    }
}