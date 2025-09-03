using Game.Detection;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using Game.Entities.Components.MotionController.Strategies;
using UnityEngine;

namespace Game.Entities.Dash
{
    /// <summary>
    /// Adds the damage strategy to the dash class. If it already has one, it overrides it.
    /// </summary>
    public class AddDashDamage : DashEffectStrategy
    {
        [SerializeField] private DashTargetConfig targetConfig;
        [SerializeField] private DashDamageConfig dashDamageConfig;
        
        protected override void OnStartEffect(Transform origin, DashHandler dash)
        {
            dash.DirStrategy.Add(DashDirEnum.Target, new DashTargetStrategy(targetConfig));
            dash.UpdateStrategy.Add(DashUpdateEnum.Damage, new DashDamageStrategy(dashDamageConfig));

            LevelManager.HasDashDamage = true;
        }

        protected override void OnStopEffect(DashHandler dash)
        {
            dash.DirStrategy.Remove(DashDirEnum.Target);
            dash.UpdateStrategy.Remove(DashUpdateEnum.Damage);
            
            LevelManager.HasDashDamage = false;
        }

        public override string Description()
        {
            return $"Adds the damage strategy to the dash class. If it already has one, it overrides it.";
        }
    }
}