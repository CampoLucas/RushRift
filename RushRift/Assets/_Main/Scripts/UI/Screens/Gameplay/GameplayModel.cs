using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class GameplayModel : UIModel
    {
        public AttributeBarModel HealthBar { get; private set; }

        public GameplayModel(AttributeBarData healthBar)
        {
            HealthBar = new AttributeBarModel(healthBar);
        }

        public override void Dispose()
        {
            base.Dispose();
            HealthBar.Dispose();
            HealthBar = null;
        }
    }
}