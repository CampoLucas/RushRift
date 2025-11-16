using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Movement/MoveDirectionDotProduct")]
    public class MoveDirectionDotProduct : MovementComboPredicate
    {
        [Header("Compare Settings")]
        [SerializeField] private CompareOperation operation;
        [SerializeField] private Direction directionToCompare;

        [Header("Values")]
        [Range(-1, 1)] [SerializeField] private float compareValue;
        [SerializeField] private float compareTolerance;
        [SerializeField] private Vector3 customDirection;
        
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var moveDir = combo.Owner.MoveDirection();
            var compareDir = GetDir(combo);
            
            var dot = Vector3.Dot(moveDir, compareDir);
            
            return Compare(dot);
        }

        private bool Compare(float dot)
        {
            switch (operation)
            {
                case CompareOperation.Equal:
                    return Mathf.Abs(compareValue - dot) < compareTolerance;
                case CompareOperation.Different:
                    return Mathf.Abs(compareValue - dot) > compareTolerance;
                case CompareOperation.Greater:
                    return compareValue > dot;
                case CompareOperation.GreaterOrEqual:
                    return compareValue >= dot;
                case CompareOperation.Less:
                    return compareValue < dot;
                case CompareOperation.LessOrEqual:
                    return compareValue <= dot;
                default:
                    return false;
            }
        }

        private Vector3 GetDir(ComboHandler combo)
        {
            switch (directionToCompare)
            {
                case Direction.EntityForward:
                    return combo.Owner.Origin.forward;
                case Direction.EntityBackwards:
                    return -combo.Owner.Origin.forward;
                case Direction.EntityUp:
                    return combo.Owner.Origin.up;
                case Direction.EntityDown:
                    return -combo.Owner.Origin.up;
                case Direction.EntityRight:
                    return combo.Owner.Origin.right;
                case Direction.EntityLeft:
                    return -combo.Owner.Origin.right;
                case Direction.Custom:
                    return customDirection;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}