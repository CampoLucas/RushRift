using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    public abstract class VariationCondition : MonoBehaviour
    {
        public abstract bool Evaluate();
    }
}