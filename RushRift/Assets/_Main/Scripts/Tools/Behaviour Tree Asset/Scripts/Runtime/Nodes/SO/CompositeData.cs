using BehaviourTreeAsset.Runtime.Node;

namespace BehaviourTreeAsset.Runtime
{
    public abstract class CompositeData : NodeData
    {
        public sealed override int ChildCapacity() => -1;
        public sealed override bool IsRoot() => false;
    }
}