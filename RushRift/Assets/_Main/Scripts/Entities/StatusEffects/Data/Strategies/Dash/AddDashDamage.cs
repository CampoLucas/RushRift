using Game.Detection;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Dash
{
    /// <summary>
    /// Adds the damage strategy to the dash class. If it already has one, it overrides it.
    /// </summary>
    public class AddDashDamage : DashEffectStrategy
    {
        [Header("Detection")]
        [SerializeField] private SphereOverlapData overlap;

        [Header("Damage")]
        [SerializeField] private float damage;
        
        protected override void OnStartEffect(Transform origin, DashComponent dash)
        {
            dash.SetUpdateStrategy(new DashDamage(origin, overlap, damage));
        }

        protected override void OnStopEffect(DashComponent dash)
        {
            dash.SetUpdateStrategy(null);
        }

        public override string Description()
        {
            return $"Adds the damage strategy to the dash class. If it already has one, it overrides it.";
        }
    }
}