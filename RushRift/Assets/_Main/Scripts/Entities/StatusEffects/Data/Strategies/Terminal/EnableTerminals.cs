using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.Dash
{
    public class EnableTerminals : EffectStrategy
    {
        public override void StartEffect(IController controller)
        {
            LevelManager.CanUseTerminal = true;
        }

        public override void StopEffect(IController controller)
        {
            LevelManager.CanUseTerminal = false;
        }
    }
}