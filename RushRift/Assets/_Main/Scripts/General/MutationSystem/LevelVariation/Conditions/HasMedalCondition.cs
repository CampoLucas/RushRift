using Game.Levels;
using Game.Saves;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Conditions/Medals/Has Medal")]
    public class HasMedalCondition : VariationCondition
    {
        [SerializeField] private MedalType medal;
        public override bool Evaluate()
        {
            return SaveSystem.LoadGame().IsMedalUnlocked(GlobalLevelManager.CurrentLevel.Get().LevelID, medal);
        }
    }
}