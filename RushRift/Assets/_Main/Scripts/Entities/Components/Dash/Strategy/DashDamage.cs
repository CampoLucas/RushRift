using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    public class DashDamage : IDashUpdateStrategy
    {
        private IDetection _detection;

        public DashDamage(Transform origin, IDetectionData detectData)
        {
            _detection = detectData.Get(origin);
        }
        
        public void OnDashUpdate(Transform transform, Vector3 currentPosition)
        {
            throw new System.NotImplementedException();
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}