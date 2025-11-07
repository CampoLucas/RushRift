using UnityEngine;

namespace Tools.Scripts.Classes
{
    [System.Serializable]
    public class Vector3Curve
    {
        public AnimationCurve x = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve y = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve z = AnimationCurve.Linear(0, 1, 1, 1);
        public float speedModifier = 1;
        public float scaleModifier = 1;
        public Vector3 axisMultiplier = Vector3.one;

        public Vector3Curve()
        {
            x = AnimationCurve.Linear(0, 1, 1, 1);
            y = AnimationCurve.Linear(0, 1, 1, 1);
            z = AnimationCurve.Linear(0, 1, 1, 1);
            speedModifier = 1;
            scaleModifier = 1;
            axisMultiplier = Vector3.one;
        }

        public Vector3Curve(AnimationCurve x, AnimationCurve y, AnimationCurve z, float speed = 1, float scale = 1)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            speedModifier = speed;
            scaleModifier = scale;
            axisMultiplier = Vector3.one;
        }
        
        public Vector3Curve(AnimationCurve x, AnimationCurve y, AnimationCurve z, float speed, float scale, Vector3 multiplier)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            speedModifier = speed;
            scaleModifier = scale;
            axisMultiplier = multiplier;
        }

        public Vector3 Evaluate(float time)
        {
            var t = time * speedModifier;
            var multiplier = axisMultiplier * scaleModifier;
            return new Vector3(x.Evaluate(t) * multiplier.x, y.Evaluate(t) * multiplier.y, z.Evaluate(t) * multiplier.z);
        }
    }
}