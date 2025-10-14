namespace Game.UI.Screens
{
    public sealed class GameModesPresenter : UIPresenter<GameModesModel, GameModesView>
    {
        public override void Begin()
        {
            base.Begin();
        }

        public override bool TryGetState(out UIState state)
        {
            state = new GameModesState(this);
            return true;
        }
    }
}