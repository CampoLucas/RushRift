using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class GameplayPresenter : UIPresenter<GameplayModel, GameplayView>
    {
        private AttributeBarPresenter _healthBarPresenter;
        
        
        public GameplayPresenter(GameplayModel model, GameplayView view) : base(model, view)
        {
            _healthBarPresenter = new AttributeBarPresenter(Model.HealthBar, View.HeathBar);
        }

        public override void Begin()
        {
            base.Begin();
            _healthBarPresenter.Begin();
        }

        public override void End()
        {
            base.End();
            _healthBarPresenter.End();
        }
    }
}