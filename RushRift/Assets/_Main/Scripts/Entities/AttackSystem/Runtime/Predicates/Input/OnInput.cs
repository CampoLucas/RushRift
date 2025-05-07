using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Input/OnInput")]
    public class OnInput : InputPredicate
    {
        [SerializeField] private string input;
        [SerializeField] private bool value = true;
        
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            return combo.GetInput(input) == value;
        }
    }
}