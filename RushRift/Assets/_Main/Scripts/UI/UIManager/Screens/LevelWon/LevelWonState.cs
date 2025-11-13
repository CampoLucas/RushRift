namespace Game.UI.StateMachine
{
    public sealed class LevelWonState : UIState<LevelWonPresenter, LevelWonModel, LevelWonView>
    {
        public LevelWonState(LevelWonPresenter presenter) : base(presenter)
        {
            presenter.Init(new LevelWonModel());
        }
    }
}