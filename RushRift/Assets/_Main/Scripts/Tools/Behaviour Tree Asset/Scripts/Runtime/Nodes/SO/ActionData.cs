using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime
{
    public abstract class ActionData : NodeData
    {
        public sealed override int ChildCapacity() => 0;
        public sealed override bool IsRoot() => false;
        
    }
}