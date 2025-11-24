using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    /// <summary>
    /// Trigger when target is lost
    /// </summary>
    public class CT_TargetLost : CrosshairTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var subject = LockOnBlink.HasTargetSubject;
            if (subject == null) return null;

            return new Trigger(subject.Where(a => !a), null, true);
        }
    }
}