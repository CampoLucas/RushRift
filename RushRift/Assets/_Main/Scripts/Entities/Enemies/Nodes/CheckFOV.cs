using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class CheckFOV : ConditionalData
    {
        public FieldOfViewData FOV => fov;
        public bool IfAny => ifAny;
        
        [SerializeField] private FieldOfViewData fov;
        [SerializeField] private bool ifAny;

        protected override INode OnCreateNode()
        {
            return new CheckFOVProxy(this);
        }

        public override void OnDrawSelected(Transform origin)
        {
            fov.Draw(origin);
        }
    }
    
    public class CheckFOVProxy : Node<CheckFOV>
    {
        private IPredicate<FOVParams> _fov;
        private FOVParams _fovParams;

        private NullCheck<Transform> _origin;
        private NullCheck<Transform> _target;
        private IController _controller;
        private EnemyComponent _enemyComp;
        
        public CheckFOVProxy(CheckFOV data) : base(data)
        {
            _fov = data.FOV.GetFOV(Data.IfAny);
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override void OnStart()
        {
            if (_controller == null) return;
            if (!_origin) _origin.Set(_controller.Transform);
            if (_enemyComp == null) _controller.GetModel().TryGetComponent(out _enemyComp);
            if (_target || _enemyComp == null) return;
            if (_enemyComp.TryGetTarget(out var target)) _target.Set(target);
        }

        protected override NodeState OnUpdate()
        {
            if (_origin == false || _target == false) return NodeState.Failure;
            _fovParams = FOVParams.GetFOVParams(_origin.Get().position, _origin.Get().forward, _target.Get().position);
            return _fov.Evaluate(ref _fovParams) ? NodeState.Success : NodeState.Failure;
        }

        protected override bool TryFailure(out string message)
        {
            if (_controller == null)
            {
                message = "WARNING: CheckFOV doesn't have a controller reference. Returning failure";
                return true;
            }

            if (_enemyComp == null)
            {
                message = "WARNING: CheckFOV's controller doesn't have a EnemyComponent. Returning failure";
                return true;
            }
            
            if (!_origin)
            {
                message = "WARNING: CheckFOV doesn't have a origin reference. Returning failure";
                return true;
            }

            if (!_target)
            {
                message = "WARNING: CheckFOV doesn't have a target reference. Returning failure";
                return true;
            }

            return base.TryFailure(out message);
        }

        protected override void OnDispose()
        {
            _fov.Dispose();
            _fov = null;
            
            _origin.Dispose();
            _target.Dispose();
            _controller = null;
            _enemyComp = null;
        }
    }
}