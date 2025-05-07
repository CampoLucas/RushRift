namespace Game.UI.Screens
{
    public class GameplayState : UIStatePresenter<GameplayPresenter, GameplayModel, GameplayView>
    {
        public GameplayState(GameplayPresenter presenter) : base(presenter)
        {
        }
    }
}