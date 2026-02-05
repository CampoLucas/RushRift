using Game.Editor;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class ComboPredicate : ScriptableObject
    {
        [SerializeField] private bool invert;
        public bool Evaluate(ComboHandler combo, IAttack next)
        {
            var result = OnEvaluate(combo, next);
            return invert ? !result : result;
        }
        
        protected virtual bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            return false;
        }
    }
}