using Game.General;
using Game.Levels;
using Game.UI.Screens.Elements;

namespace Game.UI.Screens
{
    public sealed class LevelsModel : UIModel
    {
        private NullCheck<GameModeSO> _currentMode;
        private NullCheck<BaseLevelSO> _currentLevel;
        private NullCheck<LevelButton> _currentSelectedLevel;

        public void Setup(GameModeSO gameMode)
        {
            _currentMode = gameMode;
        }
    }
}