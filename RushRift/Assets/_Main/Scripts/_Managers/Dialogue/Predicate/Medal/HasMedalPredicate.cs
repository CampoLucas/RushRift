using Game.Levels;
using Game.Saves;
using MyTools.Global;
using UnityEngine;

namespace Game.Dialogue.Predicate.Medal
{
    [CreateAssetMenu(menuName = "Game/Dialogue System/Conditions/Medals/Has Medal")]
    public class HasMedalPredicate : DialoguePredicate
    {
        [SerializeField] private MedalType medal;
        
        protected override bool OnEvaluate()
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
            return SaveSystem.LoadGame().IsMedalUnlocked(level.LevelID, medal);
        }
    }
    
    
}