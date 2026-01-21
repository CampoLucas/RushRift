using System;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Saves;
using Game.UI.StateMachine;
using UnityEngine;

namespace Game.UI.Elements
{
    [DisallowMultipleComponent]
    public class UpgradePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform origin;
        [SerializeField] private UpgradeSlot slotPrefab;

        private NullCheck<ActionObserver<BaseLevelSO>> _onLoadObserver;
        
        private void Awake()
        {
            GameEntry.LoadingState.AttachOnLoad(_onLoadObserver.GetOrDefault(
                () => new ActionObserver<BaseLevelSO>(OnLoadHandler)));
        }

        private void Start()
        {
            if (GlobalLevelManager.CurrentLevel.TryGet(out var level))
            {
                OnLoadHandler(level);
            }
        }

        private void OnLoadHandler(BaseLevelSO level)
        {
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

            for (var i = 0; i < medals.Length; i++)
            {
                var medalType = medals[i];

                if (!level.TryGetMedal(medalType, out var medal) || !medal.Icon) continue;
                var s = Instantiate(slotPrefab, origin);

                var isUnlocked = SaveSystem.LoadGame().IsMedalUnlocked(level.LevelID, medalType);
                    
                s.Init(medal.Icon, isUnlocked);
            }
        }

        private void OnDestroy()
        {
            if (_onLoadObserver.TryGet(out var observer))
            {
                GameEntry.LoadingState.DetachOnLoad(observer);
            }
            
            _onLoadObserver.Dispose();
        }
    }
}