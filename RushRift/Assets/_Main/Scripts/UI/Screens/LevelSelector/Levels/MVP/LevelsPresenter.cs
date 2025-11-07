using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.UI.Screens.Elements;
using MyTools.Global;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public sealed class LevelsPresenter : UIPresenter<LevelsModel, LevelsView>
    {
        [Header("Prefab")]
        [SerializeField] private LevelButton buttonPrefab;

        [Header("References")]
        [SerializeField] private Transform container;
        [SerializeField] private Button backButton;
        

        private List<LevelButton> _spawnedLevelButtons = new();
        private ActionObserver<GameModeSO> _onModeSelected;
        private ActionObserver<GameModeSO, BaseLevelSO> _setDefaultLevel;
        private NullCheck<LevelButton> _prevSelectedButton;

        protected override void OnInit()
        {
            base.OnInit();

            _setDefaultLevel = new ActionObserver<GameModeSO, BaseLevelSO>(DefaultSelectionHandler);
            LevelSelectorMediator.LevelSelected.Attach(_setDefaultLevel);
            
            _onModeSelected = new ActionObserver<GameModeSO>(OnModeSelectedHandler);
            LevelSelectorMediator.GameModeSelected.Attach(_onModeSelected);
            
            backButton.onClick.AddListener(OnBackHandler);
        }

        public override void Begin()
        {
            base.Begin();

            if (Model.SelectedMode.TryGet(out var gameMode))
            {
                PopulateLevels(gameMode, gameMode.Levels);
            }
        }

        public override bool TryGetState(out UIState state)
        {
            state = new LevelsState(this);
            return true;
        }
        
        private void PopulateLevels(GameModeSO mode, List<BaseLevelSO> levels)
        {
            // clear old
            for (var i = 0; i < _spawnedLevelButtons.Count; i++)
            {
                var old = _spawnedLevelButtons[i];
                
                if (!old) continue;
                Destroy(old.gameObject);
            }
            
            _spawnedLevelButtons.Clear();
            
            if (levels == null || levels.Count == 0) return;

            for (var i = 0; i < levels.Count; i++)
            {
                var levelSO = levels[i];

                if (!levelSO)
                {
                    this.Log("LevelSO is null", LogType.Error);
                    continue;
                }
                
                var button = Instantiate(buttonPrefab, container);
                _spawnedLevelButtons.Add(button);
                
                var unlocked = CheckIfUnlocked(levelSO, levels, i);
                var medals = GetUnlockedMedals(levelSO);

                button.Init(levelSO, unlocked, medals);
                
                if (!unlocked) continue;

                if (Model.SelectedLevel.TryGet(out var selectedLevelSO) && selectedLevelSO.LevelID == levelSO.LevelID)
                {
                    _prevSelectedButton = button;
                    button.Select();
                }
                
                button.GetComponent<InteractiveButton>()
                    .onClick.AddListener(() =>
                    {
                        if (unlocked)
                            OnLevelSelected(mode, levelSO, button);
                    });
            }
        }
        
        private bool CheckIfUnlocked(BaseLevelSO so, List<BaseLevelSO> levels, int index)
        {
            return so.IsUnlocked(levels, index);
        }

        private int GetUnlockedMedals(BaseLevelSO so)
        {
            var data = SaveSystem.LoadGame();

            var levelID = so.LevelID;
            
            return data.GetUnlockedMedalsCount(levelID);
        }

        private void OnLevelSelected(GameModeSO mode, BaseLevelSO level, LevelButton button)
        {
            LevelSelectorMediator.LevelSelected.NotifyAll(mode, level);
            
            if (_prevSelectedButton.TryGet(out var prev))
            {
                prev.Unselect();
            }
            
            button.Select();
            Model.SetSelectedLevel(level);
            _prevSelectedButton = button;
        }

        private void OnModeSelectedHandler(GameModeSO mode)
        {
            Model.SetMode(mode);
        }
        
        private void DefaultSelectionHandler(GameModeSO defaultMode, BaseLevelSO defaultLevel)
        {
            LevelSelectorMediator.LevelSelected.Detach(_setDefaultLevel);
            Model.SetSelectedLevel(defaultLevel);
        }

        private void OnBackHandler()
        {
            NotifyAll(MenuState.GameModes);
        }

        public override void Dispose()
        {
            base.Dispose();
            
            buttonPrefab = null;
            container = null;
            
            backButton.onClick.RemoveListener(OnBackHandler);
            backButton = null;
            
            _spawnedLevelButtons.Clear();
            LevelSelectorMediator.GameModeSelected.Detach(_onModeSelected);
            
            _onModeSelected.Dispose();
            _onModeSelected = null;
        }
    }
}