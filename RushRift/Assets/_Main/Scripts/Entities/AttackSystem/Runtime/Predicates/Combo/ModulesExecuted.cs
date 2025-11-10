using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Combo/ModulesExecuted")]
    public class ModulesExecuted : ComboPredicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var current = combo.Current;
            return current != null && current.ModulesExecuted();
        }
    }
}