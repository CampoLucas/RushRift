using Game.DesignPatterns.Observers;
using Game.Detection;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Components
{
    public sealed class LaserComponent : EntityComponent
    {
        public ISubject<Vector3> SetLengthSubject { get; private set; } = new Subject<Vector3>();
        public ISubject<Vector3> OnActivateSubject { get; private set; } = new Subject<Vector3>();
        public ISubject<Vector3> OnDeactivateSubject { get; private set; } = new Subject<Vector3>();

        private LaserComponentData _data;
        private LineDetect _detection;
        private IObserver<float> _updateObserver;
        private bool _disposed;

        private Vector3 _prevEndPos;
        private Vector3 _endPos;
        private bool _isBlocked;
        private RaycastHit _blockHit;

        private bool _state;
        private bool _prevState;

        public LaserComponent(Transform origin, LaserComponentData componentData)
        {
            _data = componentData;
            _detection = componentData.GetDetection(origin);
            _state = componentData.StartOn;
            _prevState = !_state;
        }


        private void OnUpdate(float delta)
        {
            if (_state)
            {
                ActivatedUpdate();
                if (_prevState != _state)
                {
                    OnActivateSubject.NotifyAll(_endPos);
                }
                
            }
            else
            {
                if (_prevState != _state)
                {
                    OnDeactivateSubject.NotifyAll(_endPos);
                }
            }

            _prevState = _state;
        }

        private void ActivatedUpdate()
        {
            _prevEndPos = _endPos;
            var overlapping = _detection.Detect(out _endPos, out _isBlocked, out _blockHit);
            
            if (_endPos != _prevEndPos) SetLengthSubject.NotifyAll(_endPos);
            
            if (!overlapping) return;

            var overlaps = _detection.Overlaps;

            for (var i = 0; i < overlaps; i++)
            {
                var hit = _detection.Hits[i];
                
                if (!hit.collider.gameObject.TryGetComponent<IController>(out var controller)) continue;

                // if destroyable, destroy, don't do damage
                var model = controller.GetModel();
                if (_data.DestroyDestroyables &&
                    model.TryGetComponent<DestroyableComponent>(out var destroyableComponent))
                {
                    destroyableComponent.DestroyEntity();
                }
                else if (model.TryGetComponent<HealthComponent>(out var healthComponent))
                {
                    //Kill now
                    // Temp
                    healthComponent.Intakill(hit.point);
                }
            }
        }
        
        public override bool TryGetUpdate(out IObserver<float> observer)
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
        
        protected override void OnDispose()
        {
            _disposed = true;
            _updateObserver?.Dispose();
            _updateObserver = null;
            
            _detection?.Dispose();
            _detection = null;

            _data = null;
            
            SetLengthSubject.DetachAll();
            SetLengthSubject.Dispose();
            SetLengthSubject = null;
            
            OnActivateSubject.DetachAll();
            OnActivateSubject.Dispose();
            OnActivateSubject = null;
            
            OnDeactivateSubject.DetachAll();
            OnDeactivateSubject.Dispose();
            OnDeactivateSubject = null;
        }

        public override void OnDraw(Transform origin)
        {
            if (_detection == null) return;

            var color = _detection.IsOverlapping ? Color.green : _isBlocked ? Color.magenta : Color.red;
            _detection.Draw(origin, color);
        }

        public void TurnOn()
        {
            _state = true;
        }

        public void TurnOff()
        {
            _state = false;
        }
    }
}