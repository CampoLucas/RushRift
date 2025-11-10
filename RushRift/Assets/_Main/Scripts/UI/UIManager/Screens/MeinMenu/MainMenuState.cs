namespace Game.UI.StateMachine
{
    public sealed class MainMenuState : UIState<MainMenuPresenter, MainMenuModel, MainMenuView>
    {
        public MainMenuState(MainMenuPresenter presenter) : base(presenter)
        {
            var model = new MainMenuModel();
            presenter.Init(model);
        }
    }
}