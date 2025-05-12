using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class RepeatForTime : DecoratorData
    {
        public float duration = 1f;
        public bool randomDuration;
        public Vector2 randomTime;
        public bool stopOnFailure = true;
        
        protected override INode OnCreateNode()
        {
            return new RepeatForTimeProxy(this);
        }
    }

    public class RepeatForTimeProxy : Node<RepeatForTime>
    {
        private float _startTime;
        private float _duration;
        private NodeState _prevNodeState;
        
        public RepeatForTimeProxy(RepeatForTime data) : base(data)
        {
        }

        protected override void OnStart()
        {
            _startTime = Time.time;
            _duration = Data.randomDuration ? Random.Range(Data.randomTime.x, Data.randomTime.y) : Data.duration;
            _prevNodeState = NodeState.Success;
        }

        protected override NodeState OnUpdate()
        {
            _prevNodeState = GetChild().DoUpdate();
            
            if (Data.stopOnFailure && _prevNodeState == NodeState.Failure) 
                return NodeState.Failure;
            
            return Time.time - _startTime >= _duration ? NodeState.Success : NodeState.Running;

        }
    }
}