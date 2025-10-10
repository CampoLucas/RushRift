using System.Collections.Generic;
using Game.General;
using Game.Levels;
using UnityEngine;

namespace Game.LevelSelector
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField] private List<GameModeSO> gameModes;

        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Transform levelContainer;

        private NullCheck<GameModeSO> _currentMode;
        private NullCheck<BaseLevelSO> _currentLevel;

        private void PopulateModes()
        {
            
        }

        private void SelectMode(GameModeSO mode)
        {
            _currentMode = mode;
            if (_currentMode)
            {
                PopulateLevels(_currentMode.Get().Levels);
            }
        }

        private void PopulateLevels(List<BaseLevelSO> levels)
        {
            
        }
    }
}