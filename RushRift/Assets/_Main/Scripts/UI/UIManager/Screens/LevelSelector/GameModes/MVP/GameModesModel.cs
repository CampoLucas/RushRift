using Game.Levels;

namespace Game.UI.StateMachine
{
    public sealed class GameModesModel : UIModel
    {
        public NullCheck<GameModeSO> SelectedMode { get; private set; }

        public void Init(GameModeSO selectedMode)
        {
            SelectedMode = selectedMode;
        }
    }
}