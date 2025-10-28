using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/GameMode", fileName = "New GameModeConfig")]
    public class GameModeSO : SerializableSO
    {
        public string DisplayName => displayName;
        public List<BaseLevelSO> Levels => levels;
        
        [Header("Settings")]
        [SerializeField] private string displayName;
        [SerializeField] private bool forceLock;
        [SerializeField] private bool specialUnlock;
        
        [Header("Levels")]
        [SerializeField] private List<BaseLevelSO> levels;
        
        
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

        public virtual BaseLevelSO GetNextLevel(BaseLevelSO current)
        {
            var i = levels.IndexOf(current);
            if (i >= 0 && i < levels.Count - 1)
            {
                return levels[i + 1];
            }

            return null;
        }
    }
}