using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities
{
    public class OnProjectileDestroyed : EffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            return new Trigger(LevelManager.OnProjectileDestroyed, this, false);
        }

        public override bool Evaluate(ref IController args)
        {
            return false;
        }
    }
}
