using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public abstract class AttributeData<T> where T : IAttribute
    {
        public float StartValue => startValue;
        public float MaxValue => max;
        public float RegenRate => regenRate;
        
        [Header("Settings")]
        [SerializeField] private float startValue;
        [SerializeField] private float max;
        [SerializeField] private float regenRate;
        
        public abstract T GetComponent();
    }
}