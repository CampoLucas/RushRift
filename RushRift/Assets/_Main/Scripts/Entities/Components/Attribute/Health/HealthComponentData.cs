using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class HealthComponentData : AttributeData<HealthComponent>
    {
        public DieBehaviour OnZeroHealth => onZeroHealth;
        
        [Header("On Zero Health")]
        [Tooltip("What the it happens with the entity, when the health reaches zero.")]
        [SerializeField] private DieBehaviour onZeroHealth = DieBehaviour.Nothing;
        
        public override HealthComponent GetComponent()
        {
            return new HealthComponent(this);
        }
    }

    public enum DieBehaviour
    {
        Nothing,
        Destroy,
        Disable,
    }
}