using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.Saves;
using MyTools.Global;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/Level", fileName = "New Level Config")]
    public class LevelSO : SingleLevelSO
    {
        public override bool VariationsEnabled => levelVariationsEnabled;
        [SerializeField] private bool levelVariationsEnabled = false;
        
        
        public sealed override IReadOnlyList<LevelSceneRequest> GetSceneLoadRequests()
        {
            return new[]
            {
                new LevelSceneRequest(SceneName, LoadSceneMode.Additive, true)
            };
        }
        
        public override UniTask OnScenesLoadedAsync(GlobalLevelManager manager, CancellationToken ct = default)
        {
            manager.LevelIndex = 0;
            return UniTask.CompletedTask;
        }

        public override bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex)
        {
            if (currIndex == 0)
            {
                return true;
            }

            var prevLevel = levelsList[currIndex - 1];
            if (!prevLevel)
            {
                this.Log("The previous level shouldn't be null.", LogType.Warning);
                return false;
            }

            var data = SaveSystem.LoadGame();
            if (data == null)
            {
                this.Log("Couldn't find the save data.");
                return false;
            }

            return data.IsMedalUnlocked(prevLevel.LevelID, MedalType.Bronze);
        }

        public override int TryGetEffects(out Effect[] effect)
        {
            var data = SaveSystem.LoadGame();
            return data.TryGetUnlockedEffects(LevelID, out effect);
        }
    }
    
    
}

