using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    /// <summary>
    /// Trigger when aim crosses a lockable target
    /// </summary>
    public class CT_TargetFound : CrosshairTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            
            var subject = LockOnBlink.HasTargetSubject;
            if (subject == null) return null;

            return new Trigger(subject.Where(a => a), null, true);
        }
    }
}