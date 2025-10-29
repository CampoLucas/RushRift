using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(fileName = "EnableTerminals", menuName = "Game/Effects/Strategies/Enable Terminals")]
    public class EnableTerminals : EffectStrategy
    {
        public override void StartEffect(IController controller)
        {
            GlobalLevelManager.SetPowerSurge(true);
        }

        public override void StopEffect(IController controller)
        {
            GlobalLevelManager.SetPowerSurge(false);
        }
    }
}