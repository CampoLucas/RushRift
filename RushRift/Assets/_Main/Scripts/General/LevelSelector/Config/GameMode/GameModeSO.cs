using System.Collections.Generic;
using Game.Editor;
using Game.Levels;
using MyTools.Global;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/GameMode", fileName = "New GameModeConfig")]
    public class GameModeSO : ScriptableObject
    {
        public string DisplayName => displayName;
        public List<BaseLevelSO> Levels => levels;
        
        [Header("Settings")]
        [SerializeField] private string displayName;
        [SerializeField] private bool forceLock;
        [SerializeField] private bool specialUnlock;
        
        [Header("Levels")]
        [SerializeField] private List<BaseLevelSO> levels;

        [SerializeField, HideInInspector] private bool singleLevel;
        [SerializeField, HideInInspector] private BaseLevelSO nextLevel;
        [SerializeField, HideInInspector] private GameModeSO nextGameMode;
        
        
        public bool IsUnlocked()
        {
            if (forceLock)
            {
                return false;
            }
            
            if (!specialUnlock)
            {
                if (levels != null && levels.Count > 0)
                {
                    var level = levels[0];

                    return level != null && level.IsUnlocked(levels, 0);
                }

                return false;
            }
            
            // ToDo: Make it use predicates, unlocked all levels, etc.
            return false;
        }

        public virtual bool TrySetNextLevel(in GameSessionSO session)
        {
            // get teh current level index
            var i = levels.IndexOf(session.Level);
            
            // if it is still inside the current game mode
            if (i >= 0 && i < levels.Count - 1)
            {
                session.SetLevel(levels[i + 1]);
                return true;
            }
            
            // otherwise set the level to go from
            if (singleLevel && nextLevel)
            {
                session.Initialize(null, nextLevel);
                return true;
            }

            if (!singleLevel && nextGameMode)
            {
                if (nextGameMode.levels.Count == 0)
                {
                    this.Log("Trying to play a game mode without levels.", LogType.Error);
                    return false;
                }
                session.Initialize(nextGameMode, nextGameMode.levels[0]);
                return true;
            }
            
            return false;
        }

        // public virtual BaseLevelSO GetNextLevel(BaseLevelSO current)
        // {
        //     // find the current index
        //     var i = levels.IndexOf(current);
        //     
        //     // if it is still inside the current game mode
        //     if (i >= 0 && i < levels.Count - 1)
        //     {
        //         return levels[i + 1];
        //     }
        //     
        //     return null;
        // }

        public bool GetNextIsSingleLevel() => singleLevel;
        public void SetNextIsSingleLevel(bool value) => singleLevel = value;
    }
}