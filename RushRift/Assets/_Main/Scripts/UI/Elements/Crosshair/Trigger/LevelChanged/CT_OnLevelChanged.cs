using Game.DesignPatterns.Observers;
using Game.Entities;

namespace Game.UI.Elements.Crosshair.LevelChanged
{
    public class CT_LevelChanged : CrosshairTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var subject = GameEntry.LoadingState.LevelChanged;
            return subject == null ? null : new Trigger(subject, null, true);
        }
    }
}