using BehaviourTreeAsset.Runtime.Nodes;
using UnityEngine;

namespace Game.Dialogue.Predicate
{
    public abstract class DialoguePredicate : ScriptableObject, IPredicate
    {
        [SerializeField] private bool invert;

        public bool Evaluate()
        {
            var result = OnEvaluate();
            return invert ? !result : result;
        }

        protected abstract bool OnEvaluate();
        
        public void Dispose()
        {
            
        }
    }
}