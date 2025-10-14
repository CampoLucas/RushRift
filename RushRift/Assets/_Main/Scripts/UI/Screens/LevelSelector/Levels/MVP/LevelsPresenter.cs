namespace Game.UI.Screens
{
    public sealed class LevelsPresenter : UIPresenter<LevelsModel, LevelsView>
    {
        public override bool TryGetState(out UIState state)
        {
            state = new LevelsState(this);
            return true;
        }
    }
}