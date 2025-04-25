using System.Collections.Generic;
using Game.Entities.AttackSystem;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// This predicate works like an OR, if any of its predicates return true.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/SelectorPredicate")]
    public class SelectorPredicate : Predicate
    {
        [SerializeField] private SerializableSOCollection<Predicate> predicates;

        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var result = false;

            if (predicates.Count < 0) return false;
            
            for (var i = 0; i < predicates.Count; i++)
            {
                var predicate = predicates[i];
                if (predicate == null) continue;
                if (predicate.Evaluate(combo, next))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }
    }
}