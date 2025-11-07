using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class GiveEffect : ActionData
    {
        public EffectGiver EffectGiver => effectGiver;
        
        [SerializeField] private EffectGiver effectGiver;

        protected override INode OnCreateNode()
        {
            return new GiveEffectProxy(this);
        }
    }

    public class GiveEffectProxy : Node<GiveEffect>
    {
        private IController _controller;
        
        public GiveEffectProxy(GiveEffect data) : base(data)
        {
            
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override NodeState OnUpdate()
        {
            if (_controller.GetModel().TryGetComponent<EnemyComponent>(out var enemyComponent) &&
                enemyComponent.TryGetTarget(out var target) &&
                target.gameObject.TryGetComponent<IController>(out var controller))
            {
                Data.EffectGiver.ApplyEffect(controller);   
            }
            
            return NodeState.Success;
        }
    }
}