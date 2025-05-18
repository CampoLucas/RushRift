using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Dash/CanDash")]
    public class CanDash : Predicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var owner = combo.Owner;
            if (owner == null || !owner.GetModel().TryGetComponent<DashComponent>(out var dash)) return false;

            return dash.CanDash(owner);
        }
    }
}