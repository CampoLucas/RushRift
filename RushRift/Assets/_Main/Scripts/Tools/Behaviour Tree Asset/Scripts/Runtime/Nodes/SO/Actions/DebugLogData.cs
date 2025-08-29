using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Nodes
{
    public class DebugLogData : ActionData
    {
        private class DebugLog : Node<DebugLogData>
        {
            public DebugLog(DebugLogData data) : base(data)
            {
            }
            
            protected override NodeState OnUpdate()
            {
                if (Data.editorOnly)
                {
#if UNITY_EDITOR
                    Debug.Log(Data.message);
#endif
                }
                else
                {
                    Debug.Log(Data.message);
                }
                return NodeState.Success;
            }
        }
        
        public string message;
        public bool editorOnly = true;

        protected override INode OnCreateNode()
        {
            return new DebugLog(this);
        }
    }
}