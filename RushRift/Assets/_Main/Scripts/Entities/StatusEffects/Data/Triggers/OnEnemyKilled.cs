using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities
{
    public class OnEnemyKilled : EffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            return new Trigger(EnemyController.OnEnemyDeathSubject, this, false);
        }

        public override bool Evaluate(ref IController args)
        {
            return false;
        }
    }
}
