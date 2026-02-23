namespace Game.UI.StateMachine
{
    public sealed class InteractState : UIState<InteractPresenter, InteractModel, InteractView>
    {
        public InteractState(InteractPresenter presenter) : base(presenter)
        {
            var model = new InteractModel();
            presenter.Init(model);
        }
    }
}