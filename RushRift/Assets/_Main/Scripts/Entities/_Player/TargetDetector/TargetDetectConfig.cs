using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class TargetDetectConfig
    {
        public float Range => range;
        public float SphereRadius => sphereRadius;
        public QueryTriggerInteraction TriggerInteraction => triggerInteraction;

        public LayerMask IncludeLayers => includeLayers;
        public LayerMask IgnoreLayers => ignoreLayers;

        public int MaxHits => Mathf.Max(8, maxHits);
        
        [Header("Sight")]
        [SerializeField] private float range = 60f;
        [SerializeField] private float sphereRadius = .35f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Filter")]
        [SerializeField] private LayerMask includeLayers = ~0;
        [SerializeField] private LayerMask ignoreLayers = 0;

        [Header("Alloc")]
        [SerializeField] private int maxHits = 32;

        public TargetDetectComp InstantiateComponent(Transform origin, Transform forward) =>
            new TargetDetectComp(this, origin, forward);
    }
}