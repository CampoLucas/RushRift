using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    public class CrosshairStateInstance : IDisposable
    {
        public Sprite Sprite => _state.Sprite;
        public float Alpha => _state.Alpha;
        
        public event Action OnStartRequested;
        public event Action OnStopRequested;
        
        private readonly CrosshairState _state;
        private readonly TriggerCollection _start;
        private readonly TriggerCollection _stop;

        private IObserver _onStart;
        private IObserver _onStop;

        public CrosshairStateInstance(CrosshairState state, Trigger[] start, Trigger[] stop)
        {
            _state = state;
            _start = new TriggerCollection();
            foreach (var s in start)
            {
                _start.Add(s);
            }
            
            if (_start.Count > 0)
            {
                _onStart = new ActionObserver(OnStartHandler);
                _start.Attach(_onStart);
            }

            _stop = new TriggerCollection();
            foreach (var s in stop)
            {
                _stop.Add(s);
            }
        }

        public void Start()
        {
            _start.DetachAll();
            _stop.DetachAll();
            
            if (_stop.Count > 0)
            {
                _onStop = new ActionObserver(OnStopHandler);
                _stop.Attach(_onStop);
            }
        }

        public void End()
        {
            _start.DetachAll();
            _stop.DetachAll();
            
            if (_start.Count > 0)
            {
                _onStart = new ActionObserver(OnStartHandler);
                _start.Attach(_onStart);
            }
        }

        private void OnStartHandler()
        {
            OnStartRequested?.Invoke();
        }

        private void OnStopHandler()
        {
            OnStopRequested?.Invoke();
        }

        public void Dispose()
        {
            _start.Dispose();
            _stop.Dispose();
            
            _onStart.Dispose();
            _onStart = null;
            _onStop.Dispose();
            _onStop = null;
        }
    }
}