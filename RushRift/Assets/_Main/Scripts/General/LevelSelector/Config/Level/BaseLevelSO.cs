using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Levels.SingleLevel;
using MyTools.Global;
using UnityEngine;

namespace Game.Levels
{
    public abstract class BaseLevelSO : SerializableSO
    {
        public int LevelID => levelID;
        public string LevelName => levelName;
        public bool UsesMedals => medals != null && medals.Count > 0;
        
        [Header("Settings")]
        [SerializeField] private int levelID;
        [SerializeField] private string levelName;

        [Header("Medals")]
        [SerializeField] private SerializedDictionary<MedalType, Medal> medals;

        public abstract int LevelCount();
        public abstract SingleLevelSO GetLevel(int index);
        public abstract UniTask LoadAsync(GlobalLevelManager manager);
        public virtual UniTask LoadFirstAsync(GlobalLevelManager manager) => UniTask.CompletedTask;
        public virtual UniTask LoadNextAsync(GlobalLevelManager manager) => UniTask.CompletedTask;
        public abstract bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex);
        
        public Medal GetMedal(MedalType type)
        {
            if (!UsesMedals || !medals.TryGetValue(type, out var medal))
            {
                this.Log("Returning default medal", LogType.Warning);
                return default;
            }

            return medal;
            // return type switch
            // {
            //     MedalType.Bronze => bronze,
            //     MedalType.Silver => silver,
            //     MedalType.Gold   => gold,
            //     _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            // };
        }

        public bool TryGetMedal(MedalType type, out Medal medal)
        {
            if (!UsesMedals)
            {
                medal = default;
                return false;
            }
            
            medal = GetMedal(type);
            return true;
        }
    }
}