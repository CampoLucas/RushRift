using System;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class SelectorData : CompositeData
    {
        private class Selector : Node<SelectorData>
        {
            private int _index;
            private INode _currentChild;
            
            public Selector(SelectorData data) : base(data)
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
                        _index++;
                        return _index > GetChildCount() - 1 ? NodeState.Failure : NodeState.Running;
                    case NodeState.Success:
                        return NodeState.Success;
                    case NodeState.Disable:
                        _index++;
                        return _index > GetChildCount() - 1 ? NodeState.Success : NodeState.Running;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
            return new Selector(this);
        }
    }
}