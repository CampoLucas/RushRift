using Game.DesignPatterns.Observers;
using Game.Detection;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Components
{
    public class LaserComponent : IEntityComponent
    {
        private LineDetect _detection;
        private IObserver<float> _updateObserver;
        private bool _disposed;

        private Vector3 _endPos;
        private bool _isBlocked;
        private RaycastHit _blockHit;

        public LaserComponent(Transform origin, LaserComponentData componentData)
        {
            _detection = componentData.GetDetection(origin);
        }
        
        private void OnUpdate(float delta)
        {
            Debug.Log("SuperTest: Update laser");
            if (!_detection.Detect(out _endPos, out _isBlocked, out _blockHit)) return;

            var overlaps = _detection.Overlaps;

            Debug.Log("Is Overlaping");
            
            for (var i = 0; i < overlaps; i++)
            {
                var hit = _detection.Hits[i];
                
                if (!hit.collider.gameObject.TryGetComponent<IController>(out var controller)) continue;

                // if destroyable, destroy, don't do damage
                
                if (controller.GetModel().TryGetComponent<HealthComponent>(out var healthComponent))
                {
                    //Kill now
                    Debug.Log("KillPlayer");
                    // Temp
                    healthComponent.Intakill(hit.point);
                }
            }
            
            
        }
        
        public bool TryGetUpdate(out IObserver<float> observer)
        {
            if (_disposed)
            {
                observer = null;
                return false;
            }
            
            if (_updateObserver == null) _updateObserver = new ActionObserver<float>(OnUpdate);
            observer = _updateObserver;

            return observer != null;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = null;
            return false;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = null;
            return false;
        }
        
        public void Dispose()
        {
            _disposed = true;
            _updateObserver?.Dispose();
            _updateObserver = null;
            
            _detection?.Dispose();
            _detection = null;
        }

        public void OnDraw(Transform origin)
        {
            if (_detection == null) return;

            var color = _detection.IsOverlapping ? Color.green : _isBlocked ? Color.magenta : Color.red;
            _detection.Draw(origin, color);
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
    }
}