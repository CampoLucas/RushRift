using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Conditions/Medals/Compare Medals")]
    public class CompareMedalsCondition : VariationCondition
    {
        public override bool Evaluate()
        {
            return false;
        }
    }
}