using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.UI.StateMachine.Elements;
using MyTools.Global;
using UnityEngine;

namespace Game.LevelSelector
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField] private PortalPrototype portal;
        
        [Header("Game Mode")]
        [SerializeField] private Canvas gameModeCanvas;
        [SerializeField] private List<GameModeButton> gameModes;
        
        [Header("Level")]
        [SerializeField] private Canvas levelCanvas;
        [SerializeField] private LevelButton levelButtonPrefab;
        [SerializeField] private Transform levelContainer;

        private List<LevelButton> _spawnedLevelButtons = new();
        private NullCheck<GameModeSO> _currentMode;
        private NullCheck<BaseLevelSO> _currentLevel;

        private void Start()
        {
            PopulateModes();
        }

        private void PopulateModes()
        {
            levelCanvas.enabled = false;
            gameModeCanvas.enabled = true;

            for (var i = 0; i < gameModes.Count; i++)
            {
                var gmButton = gameModes[i];

                if (!gmButton)
                {
                    this.Log("GameMode Button is null.", LogType.Error);
                    continue;
                }

                var modeSO = gmButton.Data;
                if (!modeSO)
                {
                    this.Log("GameMode Button's data is null.", LogType.Error);
                    continue;
                }
                
                gmButton.Init(new ActionObserver(() => SelectMode(modeSO)));
            }
        }

        private void SelectMode(GameModeSO mode)
        {
            this.Log("Select GameMode");
            _currentMode = mode;
            if (_currentMode)
            {
                PopulateLevels(_currentMode.Get().Levels);
                levelCanvas.enabled = true;
                gameModeCanvas.enabled = false;
            }
        }

        private void PopulateLevels(List<BaseLevelSO> levels)
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
                
                var button = Instantiate(levelButtonPrefab, levelContainer);
                _spawnedLevelButtons.Add(button);
                
                var unlocked = CheckIfUnlocked(levelSO, levels, i);
                var medals = GetUnlockedMedals(levelSO);

                button.Init(levelSO, unlocked, medals);
                button.GetComponent<InteractiveButton>()
                    .onClick.AddListener(() =>
                    {
                        if (unlocked)
                            OnLevelSelected(levelSO);
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

        private void OnLevelSelected(BaseLevelSO level)
        {
            _currentLevel = level;
            portal.SetTargetSession(_currentMode, level);
        }

        public void BackToModeSelection()
        {
            levelCanvas.enabled = false;
            gameModeCanvas.enabled = true;
        }
    }
}