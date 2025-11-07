using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class GravityData // ToDo: Refactor this, there is a gravity data in the movement data and in the model
    {
        public float MaxGravityAccel => maxGravityAccel;
        
        [Header("Settings")] 
        [SerializeField] private bool customGravity;
        [SerializeField] private float gravity;
        [SerializeField] private float gravityScale;
        [SerializeField] private float maxGravityAccel;
        
        [SerializeField, Range(0f, 1f)] private float fallAirControl = 0.3f;
        [SerializeField, Range(0f, 100f)] private float airAcceleration = 5f;
        public float AirAcceleration => airAcceleration;
        public float FallAirControl => fallAirControl;
        
        public float GetValue() => (customGravity ? gravity : Physics.gravity.y) * gravityScale;
    }
}