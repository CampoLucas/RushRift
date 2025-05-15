using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class GameplayModel : UIModel
    {
        public BarModel HealthBar { get; private set; }

        public GameplayModel(AttributeBarData healthBar)
        {
            HealthBar = new BarModel(healthBar);
        }

        public override void Dispose()
        {
            base.Dispose();
            HealthBar.Dispose();
            HealthBar = null;
        }
    }
}