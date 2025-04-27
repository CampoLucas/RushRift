using System;
using BehaviourTreeAsset.Runtime;

namespace BehaviourTreeAsset.Interfaces
{
    public interface IBehaviourRunner : IDisposable
    {
        IBehaviour Behaviour { get; }

        void Init();
        void Reset();
        NodeState Update();
        bool HasBehaviour();
    }
}