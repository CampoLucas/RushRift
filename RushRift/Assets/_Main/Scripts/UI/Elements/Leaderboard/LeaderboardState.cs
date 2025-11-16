namespace Game.UI.StateMachine
{
    public sealed class LeaderboardState : UIState<LeaderboardPresenter, LeaderboardModel, LeaderboardView>
    {
        public LeaderboardState(LeaderboardPresenter presenter) : base(presenter)
        {

        }
    }
}

