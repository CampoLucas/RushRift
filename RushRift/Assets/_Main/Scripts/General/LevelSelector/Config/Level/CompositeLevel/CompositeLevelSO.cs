using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyTools.Global;
using UnityEngine;

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
        
        public sealed override IReadOnlyList<LevelSceneRequest> GetSceneLoadRequests()
        {
            // ToDo: check the index and get the next scene instead.
            var level = GetLevel(0);
            return !level ? System.Array.Empty<LevelSceneRequest>() : level.GetSceneLoadRequests();
        }
        
        public override UniTask OnScenesLoadedAsync(GlobalLevelManager manager, CancellationToken ct = default)
        {
            manager.LevelIndex = 0;
            return UniTask.CompletedTask;
        }
    }
}