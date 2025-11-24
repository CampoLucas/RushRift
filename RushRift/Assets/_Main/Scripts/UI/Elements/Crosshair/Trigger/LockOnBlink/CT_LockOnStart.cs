using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    /// <summary>
    /// Trigger when the lock starts
    /// </summary>
    public class CT_LockOnStart : CrosshairTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var subject = LockOnBlink.LockActiveSubject;
            if (subject == null) return null;

            return new Trigger(subject.Where(a => a), null, true);
        }
    }
}