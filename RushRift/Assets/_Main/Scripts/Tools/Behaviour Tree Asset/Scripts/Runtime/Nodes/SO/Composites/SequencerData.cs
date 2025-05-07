using System;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class SequencerData : CompositeData
    {
        private class Sequencer : Node<SequencerData>
        {
            private int _index;
            private INode _currentChild;
            
            public Sequencer(SequencerData data) : base(data)
            {
            }
            
            protected override void OnStart()
            {
                _index = 0;
            }

            protected override NodeState OnUpdate()
            {
                _currentChild = GetChild(_index);

                switch (_currentChild.DoUpdate())
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    case NodeState.Failure:
                        return NodeState.Failure;
                    case NodeState.Success or NodeState.Disable:
                        _index++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return _index == GetChildCount() ? NodeState.Success : NodeState.Running;
            }

            protected override void OnDispose()
            {
                _currentChild = null;
            }
            
            protected override void OnReset()
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Reset();
                }
            }
        }

        protected override INode OnCreateNode()
        {
            return new Sequencer(this);
        }
    }
}