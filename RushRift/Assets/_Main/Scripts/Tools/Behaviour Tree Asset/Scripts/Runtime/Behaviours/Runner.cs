using System;
using BehaviourTreeAsset.Interfaces;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime
{
    public class Runner : IDisposable, IBehaviourRunner
    {
        public IBehaviour Behaviour { get; private set; }
        public GameObject Owner { get; private set; }

        public Runner(IBehaviour behaviour)
        {
            Behaviour = behaviour;
            Owner = Behaviour.Owner;
        }
        
        public void Init()
        {
            Behaviour.DoAwake(Owner);
        }

        public void Reset()
        {
            Behaviour.Reset();
        }

        public NodeState Update()
        {
            if (Behaviour != null) return Behaviour.DoUpdate();
            return NodeState.Failure;
        }

        public bool HasBehaviour()
        {
            return Behaviour != null;
        }
        
        public void Dispose()
        {
            if (Behaviour != null) Behaviour.Dispose();
        }

    }
}