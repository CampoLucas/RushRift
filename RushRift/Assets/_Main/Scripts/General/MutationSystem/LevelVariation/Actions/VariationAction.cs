using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    public abstract class VariationAction : MonoBehaviour
    {
        public enum State
        {
            Success,
            Failure
        }

        public void Init()
        {
            OnInit();
        }

        public void Execute(State state)
        {
            OnExecute(state);
        }

        protected virtual void OnInit() {}
        protected abstract void OnExecute(State state);
    }
}