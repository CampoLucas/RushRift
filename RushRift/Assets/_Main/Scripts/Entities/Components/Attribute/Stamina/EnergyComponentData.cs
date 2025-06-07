using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class EnergyComponentData : AttributeData<EnergyComponent>
    {
        public float ExtraAmount => extraAmount;
        public float ExtraTime => extraTime;
        
        [Header("Extra Dash")]
        [SerializeField] private float extraAmount = 1f;
        [SerializeField] private float extraTime = 5f;
        
        public override EnergyComponent GetComponent()
        {
            return new EnergyComponent(this);
        }
    }
}