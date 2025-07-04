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
        public float Dampening => dampening;
        
        [Header("Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float duration = .3f;
        [SerializeField] private float cooldown = .3f;
        [SerializeField] private AnimationCurve speedCurve;
        [SerializeField] private float cost = 5;
        [SerializeField] private float dampening = 0.5f;

        [Header("Strategy")]
        [SerializeField] private DashStrategy strategy;

        public DashComponent GetComponent(CharacterController controller, Transform origin, Transform cameraTransform, IMovement movement)
        {
            return new DashComponent(controller, origin, cameraTransform, GetStrategy(), this, movement);
        }

        private IDashStartStrategy GetStrategy()
        {
            switch (strategy)
            {
                case DashStrategy.Directional:
                    return new DirectionalDashStart(this);
                case DashStrategy.Forward:
                    return new ForwardDashStart(this);
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