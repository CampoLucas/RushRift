using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Blink/BlinkFinished")]
    public class BlinkFinished : ComboPredicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var owner = combo.Owner;
            if (owner == null || !owner.GetModel().TryGetComponent<BlinkComponent>(out var blink)) return false;

            return blink.BlinkFinished;
        }
    }
}