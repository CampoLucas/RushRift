using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class GameplayModel : UIModel
    {
        public BarModel HealthBar { get; private set; }
        public BarModel EnergyBar { get; private set; }

        public GameplayModel(AttributeBarData healthBar, AttributeBarData energyBar)
        {
            HealthBar = new BarModel(healthBar);
            EnergyBar = new BarModel(energyBar);
        }

        public override void Dispose()
        {
            base.Dispose();
            HealthBar.Dispose();
            HealthBar = null;
            
            EnergyBar.Dispose();
            EnergyBar = null;
        }
    }
}