namespace Game.UI.Screens
{
    public sealed class GameOverState : UIStatePresenter<GameOverPresenter, GameOverModel, GameOverView>
    {
        public GameOverState(GameOverView view) : base()
        {
            var gameOverModel = new GameOverModel();
            Presenter = new GameOverPresenter(gameOverModel, view);
        }
    }
}