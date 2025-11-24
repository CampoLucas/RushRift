using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    /// <summary>
    /// Trigger when Lock ends
    /// </summary>
    public class CT_LockOnEnd : CrosshairTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var subject = LockOnBlink.LockActiveSubject;
            return subject == null ? null : new Trigger(subject.Where(a => !a), null, true);
        }
    }
}