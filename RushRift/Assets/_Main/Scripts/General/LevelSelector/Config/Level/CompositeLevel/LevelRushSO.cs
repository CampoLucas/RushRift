using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Saves;
using Game.Utils;
using MyTools.Global;
using UnityEditor;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/Rush", fileName = "New Rush Config")]
    public class LevelRushSO : CompositeLevelSO
    {
        public override async UniTask LoadFirstAsync(GlobalLevelManager manager)
        {
            if (LevelCount() == 0)
            {
                this.Log("RushSO has no levels", LogType.Warning);
                return;
            }

            var first = Levels[0];
            await manager.AwaitLoadLevelScene(first.SceneName);
            manager.LevelIndex = 0;
        }
        
        public override async UniTask LoadNextAsync(GlobalLevelManager manager)
        {
            var index = manager.LevelIndex;
            if (index >= LevelCount() - 1)
            {
                this.Log("Rush finished!");
                return;
            }

            var current = Levels[index];

            var nextIndex = index + 1;
            var next = Levels[nextIndex];
            
            // Preload next scene when triggered
            await manager.AwaitLoadLevelScene(next.SceneName, preloaded: true);

            // Wait until current level signals “end gate reached”
            await UniTask.WaitUntil(() => manager.ReachedNextZone);

            // Unload current, promote next
            await manager.WaitUnloadScene(current.SceneName);
            manager.LevelIndex = nextIndex;

            // Reset trigger
            manager.ReachedNextZone = false;
        }

        public override bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex)
        {
            var goldUnlocked = -1;
            
            var data = SaveSystem.LoadGame();
            if (data == null)
            {
                this.Log("Couldn't find the save data.");
                return false;
            }
            
            for (var i = 0; i < Levels.Count; i++)
            {
                var sector = Levels[i];
                if (!sector)
                {
                    this.Log($"The level in the index {i} in the LevelRushSO is null. Returning false", LogType.Error);
                    return false;
                }
                
                if (data.IsMedalUnlocked(sector.LevelID, MedalType.Gold))
                {
                    goldUnlocked++;
                }
            }

            return goldUnlocked == Levels.Count;
        }
    }
}