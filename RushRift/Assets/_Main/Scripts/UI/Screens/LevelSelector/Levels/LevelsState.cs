namespace Game.UI.Screens
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