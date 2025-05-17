using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class OnCompareValue : AttributeEffectTrigger
    {
        [Header("Settings")]
        [SerializeField] private Comparison comparison;
        [SerializeField] private float tolerance = .1f;

        [Header("Value 1")]
        [SerializeField] private AttributeValue firstValue;
        [SerializeField] private float firstValueCustom;

        [Header("Value 2")]
        [SerializeField] private AttributeValue secondValue;
        [SerializeField] private float secondValueCustom;
        
        public override Trigger GetTrigger(IController controller)
        {
            if (TryGetAttribute(controller, out var atr))
            {
                var subject = atr.OnValueChanged.Where(Compare);

                return new Trigger(subject, this);
            }

            return null;
        }

        public override bool Evaluate(ref IController args)
        {
            if (TryGetAttribute(args, out var atr))
            {
                return Compare(atr.Value, atr.Value, atr.MaxValue);
            }

            return false;
        }

        private bool Compare(float current, float previous, float max)
        {
            var first = GetValue(current, previous, max, firstValueCustom, firstValue);
            var second = GetValue(current, previous, max, secondValueCustom, secondValue);
            
            switch (comparison)
            {
                case Comparison.Equal:
                    return Math.Abs(first - second) < tolerance;
                case Comparison.Different:
                    return Math.Abs(first - second) > tolerance;
                case Comparison.Greater:
                    return first > second;
                case Comparison.GreaterOrEqual:
                    return first >= second;
                case Comparison.Less:
                    return first < second;
                case Comparison.LessOrEqual:
                    return first <= second;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetValue(float current, float previous, float max, float custom, AttributeValue value)
        {
            switch (value)
            {
                case AttributeValue.Current:
                    return current;
                case AttributeValue.Previous:
                    return previous;
                case AttributeValue.Max:
                    return max;
                case AttributeValue.Custom:
                    return custom;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
    
    public enum Comparison
    {
        Equal,
        Different,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }

    public enum AttributeValue
    {
        Current,
        Previous,
        Max,
        Custom,
    }
}