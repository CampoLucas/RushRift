using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Saves;
using Game.Utils;
using MyTools.Global;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

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

