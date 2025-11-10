using UnityEngine;

namespace Game.UI.StateMachine
{
    public abstract class UIPredicate : ScriptableObject, IPredicate
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
            throw new System.NotImplementedException();
        }
    }
}