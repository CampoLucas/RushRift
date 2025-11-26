using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    public class CT_BlinkEvent : CrosshairTrigger
    {
        [SerializeField] private BlinkEvent blinkEvent;
        [SerializeField] private bool invert;
        
        public override Trigger GetTrigger(IController controller)
        {
            var subject = GetSubject(invert);

            if (subject == null)
                return null;
            
            return new Trigger(subject, null, true);
        }

        private ISubject GetSubject(bool i)
        {
            switch (blinkEvent)
            {
                case BlinkEvent.LockOn:
                    return LockOnBlink.LockActiveSubject.Where(v => v != i);
                case BlinkEvent.TargetFound:
                    return LockOnBlink.HasTargetSubject.Where(v => v != i);
                case BlinkEvent.HasLockableTarget:
                    return LockOnBlink.AimHasLockableSubject.Where(v => v != i);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum BlinkEvent
    {
        LockOn, TargetFound, HasLockableTarget
    }
}