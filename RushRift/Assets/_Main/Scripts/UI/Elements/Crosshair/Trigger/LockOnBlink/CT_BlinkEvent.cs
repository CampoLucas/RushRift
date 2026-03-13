using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    public class CT_BlinkEvent : CrosshairTrigger
    {
        [SerializeField] private BlinkEvent blinkEvent;
        [SerializeField] private bool invert;
        
        public override Trigger GetTrigger(IController controller)
        {
            if (!TryGetSubject(out var subject))
            {
                return null;
            }
            
            return new Trigger(subject, null, false);
        }

        private bool TryGetSubject(out ISubject subject)
        {
            subject = null;

            if (!PlayerSpawner.Player.TryGet(out var player) ||
                !player.GetModel().TryGetComponent<BlinkComponent>(out var blink))
            {
                return false;
            }
            
            switch (blinkEvent)
            {
                case BlinkEvent.TargetFound:
                    if (!blink.TargetFound.TryGet(out var s))
                    {
                        return false;
                    }
                    subject = s;
                    break;
                case BlinkEvent.TargetLost:
                    if (!blink.TargetLost.TryGet(out s))
                    {
                        return false;
                    }
                    subject = s;
                    break;
                case BlinkEvent.BlinkStart:
                    if (!blink.BlinkStart.TryGet(out s))
                    {
                        return false;
                    }
                    subject = s;
                    break;
                case BlinkEvent.BlinkEnd:
                    if (!blink.BlinkEnd.TryGet(out s))
                    {
                        return false;
                    }
                    subject = s;
                    break;
                default:
                    this.Log("Argument Out Of Exception", LogType.Warning);
                    return false;
            }

            return true;
        }
    }

    public enum BlinkEvent
    {
        TargetFound, TargetLost, BlinkStart, BlinkEnd
        //LockOn, TargetFound, HasLockableTarget
    }
}