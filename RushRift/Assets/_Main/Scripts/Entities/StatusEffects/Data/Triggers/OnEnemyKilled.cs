using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class OnEnemyKilled : EffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            var targetSubject = GlobalEvents.EnemyDeath.ConvertToSimple();
            return new Trigger(targetSubject, this, true);
        }

        public override bool Evaluate(ref IController args)
        {
            return false;
        }
    }
}
