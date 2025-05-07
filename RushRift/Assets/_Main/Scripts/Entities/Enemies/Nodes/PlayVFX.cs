using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;

namespace Game.BehaviourTree.Nodes
{
    public class PlayVFX : ActionData
    {
        protected override INode OnCreateNode()
        {
            return new PlayVFXProxy(this);
        }
    }

    public class PlayVFXProxy : Node<PlayVFX>
    {
        public PlayVFXProxy(PlayVFX data) : base(data)
        {
        }

        protected override NodeState OnUpdate()
        {
            VFXPool.TryGetParticle(Owner.transform.position, Owner.transform.rotation,
                Owner.transform.lossyScale.magnitude * 5, out var p);
                
            return NodeState.Success;
        }
    }
}