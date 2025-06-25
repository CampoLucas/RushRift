using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class PlayVFX : ActionData
    {
        public string VFXId => vfxId;
        public float Scale => scale;
        
        [SerializeField] private string vfxId;
        [SerializeField] private float scale = 1;
        
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
            // VFXPool.TryGetParticle(Owner.transform.position, Owner.transform.rotation,
            //     Owner.transform.lossyScale.magnitude * 5, out var p);

            LevelManager.TryGetVFX(Data.VFXId, new VFXEmitterParams()
            {
                scale = Data.Scale,
                position = Owner.transform.position,
                rotation = Owner.transform.rotation,
            }, out var emitter);
            
            return NodeState.Success;
        }
    }
}