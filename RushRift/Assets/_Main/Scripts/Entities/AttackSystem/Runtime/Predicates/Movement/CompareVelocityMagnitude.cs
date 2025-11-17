using System;
using Game.Entities.Components;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Movement/CompareVelocityMagnitude")]
    public class CompareVelocityMagnitude : MovementComboPredicate
    {
        [Header("Compare Settings")]
        [SerializeField] private CompareOperation operation;

        [Header("Values")]
        [SerializeField] private float compareValue;
        [SerializeField] private float compareTolerance;

        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            if (!combo.Owner.GetModel().TryGetComponent<IMovement>(out var move))
            {
                return false;
            }
            
            var velocity = move.Velocity.magnitude;
            return Compare(velocity);
        }
        
        private bool Compare(float value)
        {
            switch (operation)
            {
                case CompareOperation.Equal:
                    return Mathf.Abs(compareValue - value) < compareTolerance;
                case CompareOperation.Different:
                    return Mathf.Abs(compareValue - value) > compareTolerance;
                case CompareOperation.Greater:
                    return compareValue > value;
                case CompareOperation.GreaterOrEqual:
                    return compareValue >= value;
                case CompareOperation.Less:
                    return compareValue < value;
                case CompareOperation.LessOrEqual:
                    return compareValue <= value;
                default:
                    return false;
            }
        }
    }
}