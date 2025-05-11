using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class EffectInstance : IEffectInstance
    {
        private IController _controller;
        
        private List<IEffectStrategy> _strategies = new();
        private List<Trigger> _startTriggers = new();
        private List<Trigger> _stopTriggers = new();
        private List<Trigger> _removeTriggers = new();

        private IObserver _onStart;
        private IObserver _onStop;
        private IObserver _onRemove;
        private IObserver<float> _update;

        private bool _started;
        private bool _stopped;
        private bool _removed;
        private bool _disposed;

        private readonly float _duration;
        private readonly bool _temporary;
        private float _elapsedTime;
        
        public EffectInstance(IEffectStrategy[] strategies, Trigger[] startTriggers,
            Trigger[] stopTriggers, Trigger[] removeTriggers)
        {
            if (strategies is { Length: > 0 }) _strategies.AddRange(strategies);
            if (startTriggers is { Length: > 0 }) _startTriggers.AddRange(startTriggers);
            if (stopTriggers is { Length: > 0 }) _stopTriggers.AddRange(stopTriggers);
            if (removeTriggers is { Length: > 0 }) _removeTriggers.AddRange(removeTriggers);

            _onStart = new ActionObserver(OnStart);
            _onStop = new ActionObserver(OnStop);
            _onRemove = new ActionObserver(OnRemove);
        }

        public EffectInstance(IEffectStrategy[] strategies, Trigger[] startTriggers,
            Trigger[] stopTriggers, Trigger[] removeTriggers, float duration) : this(strategies, startTriggers,
            stopTriggers, removeTriggers)
        {
            if (duration <= 0) return;

            _duration = duration;
            _temporary = true;

            _update = new ActionObserver<float>(OnUpdate);
        }

        public void Initialize(IController controller)
        {
            _controller = controller;
            
            if (!controller.GetModel().TryGetComponent<StatusEffectRunner>(out var statusEffectRunner))
            {
                statusEffectRunner = new StatusEffectRunner();
                controller.GetModel().TryAddComponent(statusEffectRunner);
            }
            
            statusEffectRunner.AddEffect(this);
            
            // If it has remove triggers, it subscribes the on remove observer
            if (HasRemoveTriggers())
            {
                for (var i = 0; i < _removeTriggers.Count; i++)
                {
                    var trigger = _removeTriggers[i];
                    
                    if (trigger == null) continue;
                    if (trigger.Evaluate(ref controller))
                    {
                        statusEffectRunner.RemoveEffect(this);
                        return;
                    }
                    _removeTriggers[i].Attach(_onRemove);
                }
            }
            
            // If it has start triggers, it subscribes the on start observer
            if (_startTriggers.Count > 0)
            {
                for (var i = 0; i < _startTriggers.Count; i++)
                {
                    var trigger = _startTriggers[i];
                    
                    if (trigger == null) continue;
                    if (trigger.Evaluate(ref controller))
                    {
                        OnStart();
                        return;
                    }
                    _startTriggers[i].Attach(_onStart);
                }
            }
            else // if it doesn't have, it starts the effect
            {
                OnStart();
            }
        }

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            if (_temporary && _update != null)
            {
                observer = _update;
                return true;
            }

            observer = default;
            return false;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            for (var i = 0; i < _strategies.Count; i++)
            {
                _strategies[i].Dispose();
            }

            _strategies = null;
            
            for (var i = 0; i < _startTriggers.Count; i++)
            {
                _startTriggers[i].Dispose();
            }

            _startTriggers = null;
            
            for (var i = 0; i < _stopTriggers.Count; i++)
            {
                _stopTriggers[i].Dispose();
            }

            _stopTriggers = null;
            
            for (var i = 0; i < _removeTriggers.Count; i++)
            {
                _removeTriggers[i].Dispose();
            }

            _removeTriggers = null;
            
            _onStart.Dispose();
            _onStart = null;
            
            _onStop.Dispose();
            _onStop = null;
            
            _onRemove.Dispose();
            _onRemove = null;

            _controller = null;
        }

        private void OnUpdate(float delta)
        {
            if (_elapsedTime >= _duration)
            {
                OnRemove();
                return;
            }
            _elapsedTime += delta;
        }

        private void OnStart()
        {
            if (_disposed || _started || _removed) return;
            _started = true;
            _stopped = false;

            // When the effect starts, it unsubscribes from all the start subjects
            DetachStartTriggers();
            
            // When it starts, it subscribes to all the stop subjects
            AttachStopTriggers();

            // Executes the effect logic from when the effect starts
            Start();
        }

        private void OnStop()
        {
            Debug.Log("Stop Effect");
            OnStop(true);
        }
        
        private void OnStop(bool attachTriggers)
        {
            if (_disposed || _stopped) return;
            _stopped = true;
            _started = false;
            // When the effect stops, it unsubscribes from all the stop subjects
            DetachStopTriggers();

            // When it stops, it subscribes to all the start subjects
            if (attachTriggers) AttachStartTriggers();
            
            Stop();
        }

        private void OnRemove()
        {
            if (_disposed || _removed) return;
            _removed = true;
            
            DetachStartTriggers();
            DetachStopTriggers();
            DetachRemoveTriggers();
            
            if (_started) Stop(); // it doesn't subscribe back to the start subjects
            
            if (_controller.GetModel().TryGetComponent<StatusEffectRunner>(out var statusEffectRunner))
            {
                statusEffectRunner.RemoveEffect(this);
            }
        }

        private bool HasTriggers(ref List<Trigger> triggers) => triggers != null && triggers.Count > 0;
        private bool HasStartTriggers() => HasTriggers(ref _startTriggers);
        private bool HasStopTriggers() => HasTriggers(ref _stopTriggers);
        private bool HasRemoveTriggers() => HasTriggers(ref _removeTriggers);

        private void AttachTriggers(ref List<Trigger> triggers, ref IObserver observer)
        {
            if (!HasTriggers(ref triggers)) return;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;

                trigger.Attach(observer);
            }
        }
        
        private void DetachTriggers(ref List<Trigger> triggers, ref IObserver observer)
        {
            if (!HasTriggers(ref triggers)) return;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;

                trigger.Detach(observer);
            }
        }
        
        private void AttachStartTriggers() => AttachTriggers(ref _startTriggers, ref _onStart);
        private void DetachStartTriggers() => DetachTriggers(ref _startTriggers, ref _onStart);
        private void AttachStopTriggers() => AttachTriggers(ref _stopTriggers, ref _onStop);
        private void DetachStopTriggers() => DetachTriggers(ref _stopTriggers, ref _onStop);
        private void AttachRemoveTriggers() => AttachTriggers(ref _removeTriggers, ref _onRemove);
        private void DetachRemoveTriggers() => DetachTriggers(ref _removeTriggers, ref _onRemove);

        private void Start()
        {
            // Executes the effect logic from when the effect starts
            if (_strategies is not { Count: > 0 } || _removed) return;
            
            for (var i = 0; i < _strategies.Count; i++)
            {
                _strategies[i].StartEffect(_controller);
            }
        }
        
        private void Stop()
        {
            if (_strategies is not { Count: > 0 }) return;
            
            for (var i = 0; i < _strategies.Count; i++)
            {
                _strategies[i].StopEffect(_controller);
            }
        }
    }
}