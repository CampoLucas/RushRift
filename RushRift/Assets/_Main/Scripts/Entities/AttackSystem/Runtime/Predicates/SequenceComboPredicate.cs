using Game.Entities.AttackSystem;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Sequence Predicate")]
    public class SequenceComboPredicate : ComboPredicate
    {
        [SerializeField] private SerializableSOCollection<ComboPredicate> predicates;
        
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var result = true;

            if (predicates.Count < 0) return false;
            
            for (var i = 0; i < predicates.Count; i++)
            {
                var predicate = predicates[i];
                if (predicate == null) continue;
                if (!predicate.Evaluate(combo, next))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }
    }
}