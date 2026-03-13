using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Upgrades
{
    [DisallowMultipleComponent]
    public class EnableLockOnBlink : EffectStrategy
    {
        public override void StartEffect(IController controller)
        {
            //controller.GetModel().TryAddComponent()
            
            GlobalLevelManager.SetBlink(true);
        }

        public override void StopEffect(IController controller)
        {
            GlobalLevelManager.SetBlink(false);
        }
    }
}