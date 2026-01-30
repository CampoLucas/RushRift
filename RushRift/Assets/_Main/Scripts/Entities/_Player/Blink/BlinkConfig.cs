using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class BlinkConfig
    {
        public float Range => range;
        public float SphereRadius => sphereRadius;
        public LayerMask TargetLayers => targetLayers;
        public string RequiredTag => requiredTag;
        public bool RequireLineOfSight => requireLineOfSight;
        public int MaxHits => Mathf.Max(8, maxHits);

        public float LockTime => Mathf.Max(0.01f, lockTime);
        public float RetainGrace => Mathf.Max(0f, retainGrace);

        public Vector3 BlinkOffset => blinkOffset;
        public bool OffsetIsTargetLocal => offsetIsTargetLocal;
        public bool SnapRotationToTarget => snapRotationToTarget;
        public bool ZeroVelocity => zeroVelocity;
        public bool KillOnBlink => killOnBlink;

        public float Cooldown => Mathf.Max(0f, cooldown);
        
        [Header("Targeting")]
        [SerializeField] private float range = 45f;
        [SerializeField] private float sphereRadius = 0.35f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private string requiredTag = "Enemy";
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private int maxHits = 32;

        [Header("Charge")]
        [SerializeField] private float lockTime = .30f;
        [SerializeField] private float retainGrace = .20f;

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