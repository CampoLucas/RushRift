namespace Game.UI.StateMachine
{
    public sealed class GameOverState : UIState<GameOverPresenter, GameOverModel, GameOverView>
    {
        public GameOverState(GameOverPresenter presenter) : base(presenter)
        {
            var gameOverModel = new GameOverModel();
            presenter.Init(gameOverModel);
        }
    }
}