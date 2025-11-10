using System;
using System.Collections.Generic;
using Game.Entities.Components;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Transitions/Transition")]
    public class Transition : SerializableSO
    {
        [Header("Attack")]
        [SerializeField] protected Attack to;
        
        [Header("Conditions")]
        [SerializeField] protected List<ComboPredicate> conditions;

        public virtual TransitionProxy GetProxy(IController controller) => new TransitionProxy(to.GetProxy(controller), conditions);
    }

    public class TransitionProxy : IDisposable
    {
        private IAttack _to;
        private List<ComboPredicate> _conditions;

        public TransitionProxy(IAttack to, List<ComboPredicate> conditions)
        {
            _to = to;
            _conditions = conditions;
        }

        public virtual void Do(ComboHandler comboHandler)
        {
            comboHandler.SetAttack(_to);
        }
        
        public bool Evaluate(ComboHandler comboHandler)
        {
            for (var i = 0; i < _conditions.Count; i++)
            {
                var c = _conditions[i];
                if (c == null) continue;
                if (!c.Evaluate(comboHandler, _to)) return false;
            }

            return true;
        }
        
        public void Dispose()
        {
            _to.Dispose();
            _to = null;
            
            _conditions.Clear();
            _conditions = null;
        }
    }
}