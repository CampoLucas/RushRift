using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Combo/HasProxies")]
    public class HasModules : Predicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var proxies = combo.ComboProxies;
            return proxies is { Count: > 0 };
        }
    }
}