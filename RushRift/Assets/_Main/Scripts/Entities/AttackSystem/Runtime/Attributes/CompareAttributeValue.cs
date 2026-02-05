using System;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.AttackSystem.Attributes
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Attributes/Compare Attribute Value")]
    public class CompareAttributeValue : AttributePredicate
    {
        public enum CompareType
        {
            Equal, Different, Greater, GreaterEqual, Lower, LowerEqual  
        }

        public enum ValueType
        {
            Custom, Max
        }

        [Header("Comparison")]
        [SerializeField] private CompareType compareType;
        [SerializeField] private ValueType value;
        [SerializeField] private float custom;
        [SerializeField] private float tolerance;
        
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var owner = combo.Owner;
            if (!TryGetAttribute(owner, out var attribute)) return false;
            
            return Compare(attribute.Value, GetValue(attribute));
        }

        private bool Compare(float a, float b)
        {
            switch (compareType)
            {
                case CompareType.Equal:
                    return Math.Abs(a - b) < tolerance;
                case CompareType.Different:
                    return Math.Abs(a - b) > tolerance;
                case CompareType.Greater:
                    return a > b + tolerance;
                case CompareType.GreaterEqual:
                    return a >= b - tolerance;
                case CompareType.Lower:
                    return a < b - tolerance;
                case CompareType.LowerEqual:
                    return a <= b + tolerance;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetValue(IAttribute attribute)
        {
            switch (value)
            {
                case ValueType.Custom:
                    return custom;
                case ValueType.Max:
                    return attribute.MaxValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    
    
    
}