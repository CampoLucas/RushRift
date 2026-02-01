using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class BlinkConfig
    {
        public float Range => range;
        public float RangeBoost => energyRangeBoost;
        public float RangeOffset => rangeOffset;

        public float LockTime => Mathf.Max(0.01f, lockTime);
        public float LoseDelay => Mathf.Max(0f, loseDelay);
        public float MinAimDot => minAimDot;

        public Vector3 BlinkOffset => blinkOffset;
        public bool OffsetIsTargetLocal => offsetIsTargetLocal;
        public bool SnapRotationToTarget => snapRotationToTarget;
        public bool ZeroVelocity => zeroVelocity;
        public bool KillOnBlink => killOnBlink;

        public float Cooldown => Mathf.Max(0f, cooldown);
        
        [Header("Lock")]
        [SerializeField] private float range = 45f;
        [Tooltip("The energy increases the base range bia x%.")]
        [SerializeField] private float energyRangeBoost = 5;

        [Header("Charge")]
        [SerializeField] private float lockTime = .30f;
        
        [Header("Grace")]
        [Tooltip("Offset to the range to lose the lock.")]
        [SerializeField] private float rangeOffset = 15f;
        [Tooltip("The delay until it stop locking after losing the target.")]
        [SerializeField] private float loseDelay = .2f;
        [SerializeField, Range(-1f, 1f)] private float minAimDot = .85f; 
        
        [Header("Blink")]
        [SerializeField] private Vector3 blinkOffset = new Vector3(0f, 0f, -1.25f);
        [SerializeField] private bool offsetIsTargetLocal = true;
        [SerializeField] private bool snapRotationToTarget = true;
        [SerializeField] private bool zeroVelocity = true;
        [SerializeField] private bool killOnBlink = true;
        
        [Header("Cooldown")]
        [SerializeField] private float cooldown = 1.0f;
    }
}