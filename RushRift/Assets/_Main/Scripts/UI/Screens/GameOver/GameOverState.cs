namespace Game.UI.Screens
{
    public sealed class GameOverState : UIStatePresenter<GameOverPresenter, GameOverModel, GameOverView>
    {
        public GameOverState(GameOverPresenter presenter) : base(presenter)
        {
            var gameOverModel = new GameOverModel();
            presenter.Init(gameOverModel);
        }
    }
}