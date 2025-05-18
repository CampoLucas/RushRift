using System;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class DashData
    {
        public float Distance => distance;
        public float Duration => duration;
        public float Cooldown => cooldown;
        public AnimationCurve SpeedCurve => speedCurve;
        public float Cost => cost;
        
        [Header("Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float duration = .3f;
        [SerializeField] private float cooldown = .3f;
        [SerializeField] private AnimationCurve speedCurve;
        [SerializeField] private float cost = 5;

        [Header("Strategy")]
        [SerializeField] private DashStrategy strategy;

        public DashComponent GetComponent(CharacterController controller, Transform origin, Transform cameraTransform)
        {
            return new DashComponent(controller, origin, cameraTransform, GetStrategy(), this);
        }

        private IDashStrategy GetStrategy()
        {
            switch (strategy)
            {
                case DashStrategy.Directional:
                    return new DirectionalDash(this);
                case DashStrategy.Forward:
                    return new ForwardDash(this);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public enum DashStrategy
    {
        Directional,
        Forward,
    }
}