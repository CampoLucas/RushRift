using System;
using Cysharp.Threading.Tasks;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Saves;
using Game.UI.StateMachine;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Elements
{
    [DisallowMultipleComponent]
    public class UpgradePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform origin;
        [SerializeField] private UpgradeSlot slotPrefab;

        [Header("Defaults")]
        [SerializeField] private UpgradeIcon defaultIcon;

        private NullCheck<ActionObserver<bool>> _onLoadingObserver; // Called when it starts loading.
        private NullCheck<ActionObserver<BaseLevelSO>> _onLoadObserver; // Called when the level is loaded.

        private bool _loaded = false;
        
        private void Awake()
        {
            GameEntry.LoadingState.AttachOnLoaded(_onLoadObserver.GetOrDefault(
                () => new ActionObserver<BaseLevelSO>(OnLoadHandler)));
            GameEntry.LoadingState.AttachOnLoading(_onLoadingObserver.GetOrDefault(
                () => new ActionObserver<bool>(OnLoadingHandler)));
        }

        private void Start()
        {
            Init();
            // if (GlobalLevelManager.CurrentLevel.TryGet(out var level))
            // {
            //     OnLoadHandler(level);
            // }
            // else
            // {
            //     this.Log("Couldn't log the upgrade panel, level wasn't found.", LogType.Error);
            // }
        }

        private async void Init()
        {
            if (!GlobalLevelManager.CurrentLevel.TryGet(out var level))
            {
                await UniTask.WaitUntil(() => GlobalLevelManager.CurrentLevel.TryGet(out level));
            }
            
            this.Log("Upgrade Panel Loaded");
            OnLoad(level);
        }

        private void OnLoadingHandler(bool loading)
        {
            // When changing levels the variable is reseted.
            if (!loading) return;
            _loaded = false;
        }

        private void OnLoadHandler(BaseLevelSO level)
        {
            OnLoad(level);
        }

        private void OnLoad(BaseLevelSO level)
        {
            // Can only be loaded once per level.
            if (_loaded) return;
            _loaded = true;
            var slots = origin.childCount;
            
            // Clear previous slots
            if (slots > 0)
            {
                for (var i = 0; i < slots; i++)
                {
                    Destroy(origin.GetChild(i).gameObject);
                }
            }
            
            var medals = level.GetMedalTypes();
            
            
            if (medals == null || medals.Length == 0) return;
            this.Log("Passed the return");
            for (var i = 0; i < medals.Length; i++)
            {
                var medalType = medals[i];

                if (!level.TryGetMedal(medalType, out var medal)) continue;
                var s = Instantiate(slotPrefab, origin);

                var isUnlocked = SaveSystem.LoadGame().IsMedalUnlocked(level.LevelID, medalType);
                    
                s.Init(GetIconOrDefault(medal.Icon), isUnlocked);
            }
        }

        private UpgradeIcon GetIconOrDefault(UpgradeIcon icon) => icon == null ? defaultIcon : icon;

        private void OnDestroy()
        {
            if (_onLoadObserver.TryGet(out var loadObserver))
            {
                GameEntry.LoadingState.DetachOnLoad(loadObserver);
            }

            if (_onLoadingObserver.TryGet(out var loadingObserver))
            {
                GameEntry.LoadingState.DetachOnLoading(loadingObserver);
            }
            
            _onLoadObserver.Dispose();
            _onLoadingObserver.Dispose();
        }
    }
}