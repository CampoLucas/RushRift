using Game.Levels;
using Game.Saves;
using MyTools.Global;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Conditions/Medals/Has Medal")]
    public class HasMedalCondition : VariationCondition
    {
        [SerializeField] private MedalType medal;
        public override bool Evaluate()
        {
            var save = SaveSystem.LoadGame();
            if (save == null)
            {
                this.Log("SaveData is null", LogType.Error);
                return false;
            }
            
            if (!GlobalLevelManager.CurrentLevel.TryGet(out var level))
            {
                this.Log("The level is null", LogType.Error);
                return false;
            }
            return SaveSystem.LoadGame().CanUseMedal(level.LevelID, medal);
        }
    }
}