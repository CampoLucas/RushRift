using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public abstract class AttributeData<T> where T : IAttribute
    {
        public float StartValue => startValue;
        public float MaxValue => max;
        public bool HasRegen => hasRegeneration;
        public float RegenRate => regenRate;
        public float RegenDelay => regenDelay;
        
        [Header("Settings")]
        [SerializeField] private float startValue;
        [SerializeField] private float max;

        [Header("Regeneration")]
        [SerializeField] private bool hasRegeneration;
        [SerializeField] private float regenDelay;
        [SerializeField] private float regenRate;
        
        public abstract T GetComponent();
    }
}