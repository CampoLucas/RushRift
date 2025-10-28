using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Levels.SingleLevel;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels
{
    public abstract class CompositeLevelSO : BaseLevelSO
    {
        [SerializeField] protected List<LevelSO> Levels;
        
        public sealed override int LevelCount() => Levels?.Count ?? 0;
        public sealed override SingleLevelSO GetLevel(int index)
        {
            if (index < 0 && index >= LevelCount())
            {
                this.Log("Level Index out of exception", LogType.Error);
                return null;
            }

            return Levels[index];
        }

        public sealed override async UniTask LoadAsync(GlobalLevelManager manager)
        {
            manager.LevelIndex = 0;
            await LoadFirstAsync(manager);
        }
    }
}