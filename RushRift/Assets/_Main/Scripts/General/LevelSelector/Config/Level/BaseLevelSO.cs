using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.UI.StateMachine;
using MyTools.Global;
using UnityEngine;

namespace Game.Levels
{
    public abstract class BaseLevelSO : ScriptableObject
    {
        public int LevelID => levelID;
        public string LevelName => levelName;
        public bool UsesMedals => medals is { Count: > 0 };
        public UIStateCollection UI => overrideCollection;
        public virtual bool VariationsEnabled => false;
        public LevelSelectorData LevelSelectData => levelSelectData;

        [Header("Settings")]
        [SerializeField] private int levelID;
        [SerializeField] private string levelName;

        [Header("Medals")]
        [SerializeField] private SerializedDictionary<MedalType, Medal> medals;

        [Header("UI Override")]
        [Tooltip("This scriptable object changes the UI from the level, if it is null, the game will play with the default UI.")]
        [SerializeField] private UIStateCollection overrideCollection;

        [Header("Arguments")]
        [SerializeField] private LevelArgument[] arguments;

        [Header("Level Selector")]
        [SerializeField] private LevelSelectorData levelSelectData;

        public abstract int LevelCount();
        public abstract SingleLevelSO GetLevel(int index);
        public abstract IReadOnlyList<LevelSceneRequest> GetSceneLoadRequests();
        
        public virtual UniTask OnScenesLoadedAsync(GlobalLevelManager manager, CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }
        public abstract bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex);
        
        public Medal GetMedal(MedalType type)
        {
            if (!UsesMedals || !medals.TryGetValue(type, out var medal))
            {
                return default;
            }

            return medal;
        }

        public MedalType[] GetMedalTypes()
        {
            return medals.Keys.ToArray();
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

        public abstract int TryGetEffects(out Effect[] effect);

        public bool HasArgument(LevelArgument argument)
        {
            return arguments.Contains(argument);
        }
    }
}