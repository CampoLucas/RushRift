using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Combo/AttackFinished")]
    public class AttackFinished : Predicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var current = combo.Current;
            if (current == null) return false;

            if (current.Loop || !(((combo.BeginAttackTime + current.Duration) - Time.time) <= 0)) return false;
            return true;
        }
    }
}