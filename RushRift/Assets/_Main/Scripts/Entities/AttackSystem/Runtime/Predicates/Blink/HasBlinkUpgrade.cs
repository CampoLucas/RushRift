using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Blink/Has Blink Upgrade")]
    public class HasBlinkUpgrade : ComboPredicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            return GlobalLevelManager.Blink;
        }
    }
}