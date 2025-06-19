namespace Game.UI.Screens
{
    public class LevelWonState : UIStatePresenter<LevelWonPresenter, LevelWonModel, LevelWonView>
    {
        public LevelWonState(LevelWonPresenter presenter) : base(presenter)
        {
            presenter.Init(new LevelWonModel());
        }
    }
}