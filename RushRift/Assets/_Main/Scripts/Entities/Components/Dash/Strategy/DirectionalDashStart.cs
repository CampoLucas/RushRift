using Game.Inputs;
using UnityEngine;

namespace Game.Entities.Components
{
    public class DirectionalDashStart : IDashStartStrategy
    {
        private DashData _data;

        public DirectionalDashStart(DashData data)
        {
            _data = data;
        }
        
        public void StartDash(Transform transform, Transform cameraTransform, out Vector3 start, out Vector3 end, out Vector3 dashDir)
        {
            var input = InputManager.GetValueVector(InputManager.MoveInput);

            if (input == Vector2.zero)
            {
                dashDir = Vector3.zero;
                start = end = transform.position;
                return;
            }
            
            var forward = cameraTransform.forward;
            var right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            dashDir = (forward * input.y + right * input.x).normalized;

            start = transform.position;
            end = start + dashDir * _data.Distance;
        }
        
        public void Dispose()
        {
            _data = null;
        }
    }
}