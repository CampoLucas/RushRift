using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(fileName = "EnableTerminals", menuName = "Game/Effects/Strategies/Enable Terminals")]
    public class EnableTerminals : EffectStrategy
    {
        // private static readonly int PowerUp = Shader.PropertyToID("PowerSurge");

        public override void StartEffect(IController controller)
        {
            GlobalLevelManager.SetPowerSurge(true);
            //Shader.SetGlobalFloat(PowerUp, 1);
        }

        public override void StopEffect(IController controller)
        {
            GlobalLevelManager.SetPowerSurge(false);
            //Shader.SetGlobalFloat(PowerUp, 0);
        }
    }
}