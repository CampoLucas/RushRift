using Tools.Scripts.Classes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Animations
{
    public enum UIAnimType
    {
        Move,
        Scale,
        Rotate,
        Color,
    }
    
    [System.Serializable]
    public struct UIAnim
    {
        public float Duration => duration;
        public float Delay => delay;
        public UIAnimType Type => type;
        public Vector2 TargetVector => moveAnim.Pos;
        public Vector2Curve Curve2 => moveAnim.Curve;

        public AnimationCurve Curve => type switch
        {
            UIAnimType.Scale => scaleAnim.Curve,
            UIAnimType.Rotate => rotationAnim.Curve,
            _ => colorAnim.Curve
        };

        public float TargetFloat => type == UIAnimType.Rotate ? rotationAnim.Rot : scaleAnim.Scale;
        public Color TargetColor => colorAnim.Color;
        
        [Header("Delay & Duration")]
        [SerializeField] private float duration;
        [SerializeField] private float delay;

        [Header("Type")]
        [SerializeField] private UIAnimType type;
        [SerializeField] private UIMoveAnim moveAnim;
        [SerializeField] private UIScaleAnim scaleAnim;
        [SerializeField] private UIRotationAnim rotationAnim;
        [SerializeField] private UIColorAnim colorAnim;

        private Vector2 GetTargetVector()
        {
            if (type == UIAnimType.Move)
            {
                return moveAnim.Pos;
            }
            
            return Vector2.zero;
        }
    }

    [System.Serializable]
    public struct UIMoveAnim
    {
        public Vector2 Pos => endPosition;
        public Vector2Curve Curve => curve;

        [SerializeField] private Vector2 endPosition;
        [SerializeField] private Vector2Curve curve;
    }

    [System.Serializable]
    public struct UIScaleAnim
    {
        public float Scale => endScale;
        public AnimationCurve Curve => curve;

        [SerializeField] private float endScale;
        [SerializeField] private AnimationCurve curve;
    }

    [System.Serializable]
    public struct UIColorAnim
    {
        public Color Color => endColor;
        public AnimationCurve Curve => curve;
        
        [SerializeField] private Color endColor;
        [SerializeField] private AnimationCurve curve;
    }

    [System.Serializable]
    public struct UIRotationAnim
    {
        public float Rot => endRotation;
        public AnimationCurve Curve => curve;

        [SerializeField] private float endRotation;
        [SerializeField] private AnimationCurve curve;
    }

    [System.Serializable]
    public struct SequenceAnim
    {
        [Tooltip("If true, this sequence will repeat forever.")]
        public bool looping;
        
        [Tooltip("Animations inside this sequence are played in parallel.")]
        public UIAnim[] anims;
    }
}