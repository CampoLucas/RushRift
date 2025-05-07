using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class WaitData : ActionData
    {
        private class Wait : Node<WaitData>
        {
            private float _startTime;
            private float _duration;
            
            public Wait(WaitData data) : base(data)
            {
            }
            
            protected override void OnStart()
            {
                _startTime = Time.time;
                _duration = Data.randomDuration ? Random.Range(Data.randomTime.x, Data.randomTime.y) : Data.duration;
            }

            protected override NodeState OnUpdate()
            {
                return Time.time - _startTime >= _duration ? NodeState.Success : NodeState.Running;
            }
        }
        
        public float duration = 1f;
        public bool randomDuration;
        public Vector2 randomTime;

        protected override INode OnCreateNode()
        {
            return new Wait(this);
        }
    }
}