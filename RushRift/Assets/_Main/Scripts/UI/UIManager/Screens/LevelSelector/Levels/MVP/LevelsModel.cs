using Game.General;
using Game.Levels;
using Game.UI.StateMachine.Elements;

namespace Game.UI.StateMachine
{
    public sealed class LevelsModel : UIModel
    {
        public NullCheck<GameModeSO> SelectedMode { get; private set; }
        public NullCheck<BaseLevelSO> SelectedLevel { get; private set; }

        public void SetMode(GameModeSO gameMode)
        {
            SelectedMode = gameMode;
        }
        
        public void SetSelectedLevel(BaseLevelSO level)
        {
            SelectedLevel = level;
        }
    }
}