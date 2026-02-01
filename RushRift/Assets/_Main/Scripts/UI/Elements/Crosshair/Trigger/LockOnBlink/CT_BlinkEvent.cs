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
            
            return new Trigger(subject, null, true);
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
                    subject = blink.OnTargetFound;
                    break;
                case BlinkEvent.TargetLost:
                    subject = blink.OnTargetLost;
                    break;
                case BlinkEvent.BlinkStart:
                    subject = blink.OnBlinkStart;
                    break;
                case BlinkEvent.BlinkEnd:
                    subject = blink.OnBlinkEnd;
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