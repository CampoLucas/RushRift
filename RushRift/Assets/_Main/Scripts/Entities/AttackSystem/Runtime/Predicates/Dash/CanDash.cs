using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Dash/CanDash")]
    public class CanDash : Predicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var owner = combo.Owner;
            if (owner == null || !owner.GetModel().TryGetComponent<MotionController>(out var motion) ||
                !motion.TryGetHandler<DashHandler>(out var dash)) return false;

            return dash.CanDash(owner);
        }
    }
}