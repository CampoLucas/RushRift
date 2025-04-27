namespace Game.UI.Screens
{
    public class GameOverState : UIStatePresenter<GameOverPresenter, GameOverModel, GameOverView>
    {
        public GameOverState(GameOverPresenter presenter) : base(presenter)
        {
        }
    }
}