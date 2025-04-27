using BehaviourTreeAsset.Runtime.Interfaces;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class PlayAnimationData : AnimationData
    {
        private class PlayAnimation : Animation<PlayAnimationData>
        {
            public PlayAnimation(PlayAnimationData data) : base(data)
            {
            }

            protected override NodeState OnUpdate()
            {
                if (base.OnUpdate() == NodeState.Failure) return NodeState.Failure;

                if (Data.layer < 0)
                {
                    Play(Data.animation);
                }
                else
                {
                    Play(Data.animation, Data.layer);
                }
                
                return NodeState.Success;
            }
        }

        public string Anim => animation;
        
        [SerializeField] private string animation;
        [SerializeField] private int layer = -1;

        protected override INode OnCreateNode()
        {
            return new PlayAnimation(this);
        }
    }
}