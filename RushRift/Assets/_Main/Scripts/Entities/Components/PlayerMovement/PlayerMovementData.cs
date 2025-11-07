using System;
using System.Collections.Generic;
using Game.Detection;
using Game.Tools;
using MyTools.Global;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class PlayerMovementData
    {
        public float Gravity => gravity;
        public float CoyoteTime => coyoteTime;
        public IDetectionData Detector => detector;
        
        [Header("Settings")]
        [SerializeField] private ProfileContainer[] profiles;
        [SerializeField] private float gravity = -15f;

        [Space(10), Header("Coyote Time")]
        [SerializeField, Range(0f, .5f)] private float coyoteTime = .15f;

        [Space(10), Header("Ground Check")]
        [SerializeField] private SphereOverlapData detector;

        public IMovement GetMovement(CharacterController controller, Transform origin, Transform orientation) =>
            new PlayerMovement(this, controller, origin, orientation);

        public bool TryCreateProfilesDictionary(out Dictionary<MoveType, MovementProfile> profilesDict)
        {
            profilesDict = new();
            
            if (profiles == null || profiles.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: The PlayerMovementData class doesn't any MovementProfiles.");
#endif
                return false;
            }

            for (var i = 0; i < profiles.Length; i++)
            {
                var container = profiles[i];
                if (profilesDict.ContainsKey(container.type))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"WARNING: The PlayerMovementData has the type {container.type} duplicated.");
#endif
                    continue;
                }
                
                profilesDict[container.type] = container.profile;
            }

            return true;
        }
    }

    [System.Serializable]
    public class MovementProfile
    {
        public float MaxSpeed => maxSpeed;
        public float Accel => acceleration;
        public float Dec => deceleration;
        public float Control => control;
        public AnimationCurve AccelCurve => accelerationCurve;
        public AnimationCurve DecCurve => decelerationCurve;
        
        [Header("Raw Values")]
        [SerializeField] private float maxSpeed = 10;
        [SerializeField] private float acceleration = 15;
        [SerializeField] private float deceleration = 30;
        [SerializeField] [Range(0f, 1f)] private float control = 1;
        
        [Header("Curve Modifiers")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public enum MoveType
    {
        Grounded, Air
    }

    [System.Serializable]
    public struct ProfileContainer
    {
        public MoveType type;
        public MovementProfile profile;
    }
}