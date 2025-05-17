using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class GameplayPresenter : UIPresenter<GameplayModel, GameplayView>
    {
        private BarPresenter _healthBarPresenter;
        private BarPresenter _energyBarPresenter;
        
        
        public GameplayPresenter(GameplayModel model, GameplayView view) : base(model, view)
        {
            _healthBarPresenter = new BarPresenter(Model.HealthBar, View.HeathBar);
            _energyBarPresenter = new BarPresenter(model.EnergyBar, View.EnergyBar);
        }

        public override void Begin()
        {
            base.Begin();
            _healthBarPresenter.Begin();
            _energyBarPresenter.Begin();
        }

        public override void End()
        {
            base.End();
            _healthBarPresenter.End();
            _energyBarPresenter.End();
        }

        public override void Dispose()
        {
            _healthBarPresenter.Dispose();
            _healthBarPresenter = null;
            
            _energyBarPresenter.Dispose();
            _energyBarPresenter = null;
            
            base.Dispose();
        }
    }
}