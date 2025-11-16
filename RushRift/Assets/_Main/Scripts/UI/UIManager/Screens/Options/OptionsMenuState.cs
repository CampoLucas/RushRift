namespace Game.UI.StateMachine
{
    public sealed class OptionsMenuState : UIState<OptionsPresenter, OptionsModel, OptionsView>
    {
        public OptionsMenuState(OptionsPresenter presenter) : base(presenter)
        {
            var model = new OptionsModel();
            presenter.Init(model);
        }
    }
}