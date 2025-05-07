using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using BehaviourTreeAsset.Utils;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public abstract class AnimationData : ActionData
    {
        public class Animation<TData> : Node<TData> where TData : AnimationData
        {
            private Animator _animator;
            private GameObject _prevOwner;
        
            public Animation(TData data) : base(data)
            {
            }

            protected override void OnStart()
            {
                var owner = GetDefault(Data.Target);
                
                if (_prevOwner != owner)
                {
                    _prevOwner = owner;
                    _animator = owner.GetComponent<Animator>();
                    if (Data.SearchInChildren && !_animator)
                    {
                        _animator = owner.GetComponentInChildren<Animator>();
                    }
                }
            }

            protected override NodeState OnUpdate()
            {
                if (!_animator)
                {
                    DebugLogger.EditorLog("The animator in the animation node is null", Owner, DebugLogger.LogType.Warning);
                    return NodeState.Failure;
                }

                return NodeState.Success;
            }

            protected void Play(string animation, int layer)
            {
                _animator.Play(animation, layer);
            }
            
            protected void Play(string animation)
            {
                _animator.Play(animation);
            }
        }

        public bool SearchInChildren => searchInChildren;
        public GameObject Target => target;
        
        [SerializeField] private GameObject target;
        [SerializeField] private bool searchInChildren;

    }
    
    
}