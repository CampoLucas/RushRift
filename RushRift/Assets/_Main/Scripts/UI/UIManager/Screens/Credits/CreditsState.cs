namespace Game.UI.StateMachine
{
    public sealed class CreditsState : UIState<CreditsPresenter, CreditsModel, CreditsView>
    {
        public CreditsState(CreditsPresenter presenter) : base(presenter)
        {
            var model = new CreditsModel();
            presenter.Init(model);
        }
    }
}