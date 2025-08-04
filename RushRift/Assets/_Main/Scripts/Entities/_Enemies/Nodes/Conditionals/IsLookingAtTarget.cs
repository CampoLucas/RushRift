using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class IsLookingAtTarget : ConditionalData
    {
        public float Threshold => threshold;
        public bool UseJoints => useJoins;
        public EntityJoint Joint => joint;
        
        [SerializeField] private float threshold = .95f;
        
        [Header("Joints")]
        [SerializeField] private bool useJoins;
        [SerializeField] private EntityJoint joint;
        
        protected override INode OnCreateNode()
        {
            return new IsLookingAtTargetProxy(this);
        }
    }

    public class IsLookingAtTargetProxy : Node<IsLookingAtTarget>
    {
        private NullCheck<Transform> _origin;
        private NullCheck<Transform> _target;
        private IController _controller;
        private EnemyComponent _enemyComp;
        
        public IsLookingAtTargetProxy(IsLookingAtTarget data) : base(data)
        {
        }

        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }
        
        protected override void OnStart()
        {
            if (_controller == null) return;
            if (!_origin)
            {
                _origin.Set(Data.UseJoints ? _controller.Joints.GetJoint(Data.Joint) : _controller.Origin);
            }
            if (_enemyComp == null) _controller.GetModel().TryGetComponent(out _enemyComp);
            if (_target || _enemyComp == null) return;
            if (_enemyComp.TryGetTarget(out var target)) _target.Set(target);
        }
        
        protected override NodeState OnUpdate()
        {
            if (_origin == false || _target == false) return NodeState.Failure;
            var origin = _origin.Get();
            return IsLookingAt(origin.position, origin.forward, _target.Get().position) ? NodeState.Success : NodeState.Failure;
        }

        public bool IsLookingAt(Vector3 origin, Vector3 forward, Vector3 targetPosition, float threshold = 0.95f)
        {
            var toTarget = (targetPosition - origin).normalized;
            var dot = Vector3.Dot(forward.normalized, toTarget);
            return dot >= threshold;
        }

        protected override void OnDispose()
        {
            _origin.Dispose();
            _target.Dispose();
            _controller = null;
            _enemyComp = null;
        }
    }
}