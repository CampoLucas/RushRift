namespace Game.UI.Screens
{
    public sealed class GameModesState : UIState<GameModesPresenter, GameModesModel, GameModesView>
    {
        public GameModesState(GameModesPresenter presenter) : base(presenter)
        {
            var model = new GameModesModel();
            presenter.Init(model);
        }
    }
}