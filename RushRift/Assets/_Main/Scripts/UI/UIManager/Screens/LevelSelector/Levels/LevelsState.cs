namespace Game.UI.StateMachine
{
    public sealed class LevelsState : UIState<LevelsPresenter, LevelsModel, LevelsView>
    {
        public LevelsState(LevelsPresenter presenter) : base(presenter)
        {
            var model = new LevelsModel();
            presenter.Init(model);
        }
    }
}