using UnityEngine;

namespace Game.Entities.Components
{
    public class ForwardDashStart : IDashStartStrategy
    {
        private DashData _data;

        public ForwardDashStart(DashData data)
        {
            _data = data;
        }
        
        public void StartDash(Transform transform, Transform cameraTransform, out Vector3 start, out Vector3 end, out Vector3 dashDir)
        {
            dashDir = cameraTransform.forward;

            if (dashDir.sqrMagnitude > 0f)
            {
                dashDir.Normalize();
                start = transform.position;
                end = start + dashDir * _data.Distance;
            }
            else
            {
                start = end = transform.position;
            }
        }

        public void Dispose()
        {
            _data = null;
        }
    }
}