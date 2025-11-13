using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Transitions/ExitTransition")]
    public class ExitTransition : Transition
    {
        public override TransitionProxy GetProxy(IController controller)
        {
            return new ExitTransitionProxy(null, conditions);
        }
    }
    
    public class ExitTransitionProxy : TransitionProxy
    {
        public ExitTransitionProxy(IAttack to, List<ComboPredicate> conditions) : base(to, conditions)
        {
        }

        public override void Do(ComboHandler comboHandler)
        {
            comboHandler.StopCombo();
        }
    }
}