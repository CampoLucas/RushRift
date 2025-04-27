using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using Game.Entities;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.BehaviourTree.Nodes
{
    public class Rotate : ActionData
    {
        public bool Instant => instant;
        public float RotationSpeed => rotationSpeed;
        public float YOffset => yOffset;
        public bool Eyes => eyes;
        public bool LockY => lockY;
        
        [Header("Rot Settings")]
        [SerializeField] private bool instant;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private bool eyes;
        [SerializeField] private bool lockY;

        [Header("Target Settings")]
        [SerializeField] private float yOffset;

        protected override INode OnCreateNode()
        {
            return new RotateProxy(this);
        }
    }

    public class RotateProxy : Node<Rotate>
    {
        private NullCheck<Transform> _origin;
        private NullCheck<Transform> _target;
        private IController _controller;
        private EnemyComponent _enemyComp;
        
        public RotateProxy(Rotate data) : base(data)
        {
        }
        
        protected override void OnAwake()
        {
            _controller = Owner.GetComponent<IController>();
        }

        protected override void OnStart()
        {
            if (_controller == null) return;
            if (!_origin) _origin.Set(Data.Eyes ? _controller.EyesTransform : _controller.Transform);
            if (_enemyComp == null) _controller.GetModel().TryGetComponent(out _enemyComp);
            if (_target || _enemyComp == null) return;
            if (_enemyComp.TryGetTarget(out var target)) _target.Set(target);
        }

        protected override NodeState OnUpdate()
        {
            Rotate(((_target.Get().position + (Vector3.up * Data.YOffset)) - _origin.Get().position).normalized, Data.RotationSpeed, Time.deltaTime);

            return NodeState.Success;
        }

        protected override bool TryFailure(out string message)
        {
            if (_controller == null)
            {
                message = "WARNING: Rotate doesn't have a controller reference. Returning failure";
                return true;
            }

            if (_enemyComp == null)
            {
                message = "WARNING: Rotate's controller doesn't have a EnemyComponent. Returning failure";
                return true;
            }
            
            if (!_origin)
            {
                message = "WARNING: Rotate doesn't have a origin reference. Returning failure";
                return true;
            }

            if (!_target)
            {
                message = "WARNING: Rotate doesn't have a target reference. Returning failure";
                return true;
            }

            return base.TryFailure(out message);
        }
        
        protected override void OnDispose()
        {
            _origin.Dispose();
            _target.Dispose();
            _controller = null;
            _enemyComp = null;
        }

        private void Rotate(Vector3 dir, float speed, float delta)
        {
            dir.y = 0;
            var tr = Quaternion.LookRotation(dir);

            _origin.Get().rotation = Data.Instant ? tr : Quaternion.Slerp(_origin.Get().rotation, tr, speed * delta);
        }
    }
}