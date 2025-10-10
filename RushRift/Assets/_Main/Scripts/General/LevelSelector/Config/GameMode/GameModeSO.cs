using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.General
{
    [CreateAssetMenu(menuName = "Game/GameMode", fileName = "New GameModeConfig")]
    public class GameModeSO : SerializableSO
    {
        public List<BaseLevelSO> Levels => levels;
        
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<BaseLevelSO> levels;

        [SerializeField] private bool specialUnlock;
        
        public bool IsUnlocked()
        {
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
    }
}