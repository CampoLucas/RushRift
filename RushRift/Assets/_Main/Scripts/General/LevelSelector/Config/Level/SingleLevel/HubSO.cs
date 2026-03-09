using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/Hub", fileName = "New Hub Config")]
    public sealed class HubSO : LevelSO
    {
        public override bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex)
        {
            return true;
        }
    }
    
    
}

