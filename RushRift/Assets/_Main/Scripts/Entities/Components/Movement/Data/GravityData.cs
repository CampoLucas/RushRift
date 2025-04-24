using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class GravityData // ToDo: Refactor this, there is a gravity data in the movement data and in the model
    {
        [Header("Settings")] 
        [SerializeField] private bool customGravity;
        [SerializeField] private float gravity;
        [SerializeField] private float gravityScale;

        public float GetValue() => (customGravity ? gravity : Physics.gravity.y) * gravityScale;
    }
}