using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using BehaviourTreeAsset.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace BehaviourTreeAsset.Runtime.Nodes.SubBehaviour
{
    public class RunBehaviourData : ActionData
    {
        public bool WaitUntilFinished => waitUntilFinished;
        
        [SerializeField] private BehaviourTreeData behaviour;
        [SerializeField] private bool waitUntilFinished; // if true, it doesn't enable it and returns running, if false, it only enables it and returns true
        //[SerializeField] private bool restartWhenRunning;
        
        protected override INode OnCreateNode()
        {
            return new RunBehaviour(this);
        }

        public bool TryGetSubTree(out BehaviourTreeData subTreeData)
        {
            subTreeData = null;
            if (behaviour == null || behaviour == behaviourData) return false;
            subTreeData = behaviour;
            return true;
        }
    }
    
    public class RunBehaviour : Node<RunBehaviourData>
    {
        private IBehaviourRunner _runner;
            
        public RunBehaviour(RunBehaviourData data) : base(data) { }

        protected override void OnStart()
        {
            if (Data.TryGetSubTree(out var tree))
            {
                OwnerBehaviour.Runner.AddOrGet(tree, out _runner, !Data.WaitUntilFinished);
            }

            if (_runner != null && (Data.WaitUntilFinished)) //ToDo: (Data.WaitUntilFinished || _runner.CurrentState != NodeState.Running)
            {
                _runner.Reset();
            }
        }

        protected override NodeState OnUpdate()
        {
            if (Data.WaitUntilFinished)
            {
                return _runner.Update();
            }
            
            return NodeState.Success;
        }

        protected override bool TryFailure(out string message)
        {
            if (_runner == null)
            {
                message = "WARNING: The runner class is null, returning failure";
                return true;
            }
            
            if (!_runner.HasBehaviour())
            {
                message = "WARNING: The runner class behaviour is null, returning failure";
                return true;
            }
            
            return base.TryFailure(out message);
        }

        protected override void OnDispose()
        {
            
        }
    }
}