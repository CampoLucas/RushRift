using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class OnProjectileDestroyed : EffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var targetSubject = GlobalEvents.ProjectileDestroyed.ConvertToSimple();
            return new Trigger(targetSubject, this, true);
        }

        public override bool Evaluate(ref IController args)
        {
            return false;
        }
    }
}
