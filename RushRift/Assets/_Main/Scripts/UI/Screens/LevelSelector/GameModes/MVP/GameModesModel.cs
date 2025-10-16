using Game.General;

namespace Game.UI.Screens
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