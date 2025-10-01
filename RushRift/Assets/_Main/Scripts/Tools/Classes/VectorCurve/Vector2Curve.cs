using UnityEngine;

namespace Tools.Scripts.Classes
{
    [System.Serializable]
    public class Vector2Curve
    {
        public AnimationCurve x = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve y = AnimationCurve.Linear(0, 1, 1, 1);
        public float speedModifier = 1;
        public float scaleModifier = 1;
        public Vector2 axisMultiplier = Vector2.one;

        public Vector2Curve()
        {
            x = AnimationCurve.Linear(0, 1, 1, 1);
            y = AnimationCurve.Linear(0, 1, 1, 1);
            speedModifier = 1;
            scaleModifier = 1;
            axisMultiplier = Vector2.one;
        }

        public Vector2Curve(AnimationCurve x, AnimationCurve y, float speed = 1, float scale = 1)
        {
            this.x = x;
            this.y = y;
            speedModifier = speed;
            scaleModifier = scale;
            axisMultiplier = Vector2.one;
        }
        
        public Vector2Curve(AnimationCurve x, AnimationCurve y, float speed, float scale, Vector2 multiplier)
        {
            this.x = x;
            this.y = y;
            speedModifier = speed;
            scaleModifier = scale;
            axisMultiplier = multiplier;
        }

        public Vector2 Evaluate(float time)
        {
            var t = time * speedModifier;
            var multiplier = axisMultiplier * scaleModifier;
            return new Vector3(x.Evaluate(t) * multiplier.x, y.Evaluate(t) * multiplier.y);
        }
    }
}