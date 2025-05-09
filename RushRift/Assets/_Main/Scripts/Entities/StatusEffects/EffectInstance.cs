using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities
{
    public class EffectInstance : IEffectInstance
    {
        private IController _controller;
        
        private List<IEffectStrategy> _strategies = new();
        private List<ISubject> _startTriggers = new();
        private List<ISubject> _stopTriggers = new();
        private List<ISubject> _removeTriggers = new();

        private IObserver _onStart;
        private IObserver _onStop;
        private IObserver _onRemove;

        private bool _started;
        private bool _stopped;
        private bool _removed;
        

        public EffectInstance(IEffectStrategy[] strategies, ISubject[] startTriggers,
            ISubject[] stopTriggers, ISubject[] removeTriggers)
        {
            if (strategies is { Length: > 0 }) _strategies.AddRange(strategies);
            if (startTriggers is { Length: > 0 }) _startTriggers.AddRange(startTriggers);
            if (stopTriggers is { Length: > 0 }) _stopTriggers.AddRange(stopTriggers);
            if (removeTriggers is { Length: > 0 }) _removeTriggers.AddRange(removeTriggers);

            _onStart = new ActionObserver(OnStart);
            _onStop = new ActionObserver(OnStop);
            _onRemove = new ActionObserver(OnRemove);
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
            if (_removeTriggers.Count > 0)
            {
                for (var i = 0; i < _removeTriggers.Count; i++)
                {
                    _removeTriggers[i].Attach(_onRemove);
                }
            }
            
            // If it has start triggers, it subscribes the on start observer
            if (_startTriggers.Count > 0)
            {
                for (var i = 0; i < _startTriggers.Count; i++)
                {
                    _startTriggers[i].Attach(_onStart);
                }
            }
            else // if it doesn't have, it starts the effect
            {
                OnStart();
            }
        }

        private void OnStart()
        {
            if (_started) return;
            if (_removed) return;
            _started = true;
            _stopped = false;

            if (_startTriggers.Count > 0)
            {
                // When the effect starts, it unsubscribes from all the start subjects
                for (var i = 0; i < _startTriggers.Count; i++)
                {
                    _startTriggers[i].Detach(_onStart);
                }
            }

            if (_stopTriggers.Count > 0)
            {
                // When it starts, it subscribes to all the stop subjects
                for (var i = 0; i < _stopTriggers.Count; i++)
                {
                    _stopTriggers[i].Attach(_onStop);
                }
            }

            if (_strategies is { Count: > 0 })
            {
                // Executes the effect logic from when the effect starts
                if (_strategies == null) Debug.Log("Strategy is null");
                for (var i = 0; i < _strategies.Count; i++)
                {
                    _strategies[i].StartEffect(_controller);
                }
            }
        }

        private void OnStop()
        {
            Debug.Log("Stop Effect");
            OnStop(true);
        }
        
        private void OnStop(bool attachTriggers)
        {
            if (_stopped) return;
            _stopped = true;
            _started = false;
            // When the effect stops, it unsubscribes from all the stop subjects
            if (_stopTriggers.Count > 0)
            {
                for (var i = 0; i < _stopTriggers.Count; i++)
                {
                    _stopTriggers[i].Detach(_onStop);
                }
            }

            if (attachTriggers && _startTriggers.Count > 0)
            {
                // When it stops, it subscribes to all the start subjects
                for (var i = 0; i < _startTriggers.Count; i++)
                {
                    _startTriggers[i].Attach(_onStart);
                }
            }
            
            if (_strategies != null && _strategies.Count > 0)
            {
                for (var i = 0; i < _strategies.Count; i++)
                {
                    _strategies[i].StopEffect(_controller);
                }
            }

            
        }

        private void OnRemove()
        {
            if (_removed) return;
            _removed = true;
            
            if (_started) OnStop(false); // it doesn't subscribe back to the start subjects
            
            
            if (_startTriggers.Count > 0)
            {
                for (var i = 0; i < _startTriggers.Count; i++)
                {
                    _startTriggers[i].Detach(_onStart);
                }
            }
            
            if (_stopTriggers.Count > 0)
            {
                for (var i = 0; i < _stopTriggers.Count; i++)
                {
                    _stopTriggers[i].Detach(_onStop);
                }
            }
            
            if (_removeTriggers.Count > 0)
            {
                for (var i = 0; i < _removeTriggers.Count; i++)
                {
                    _removeTriggers[i].Detach(_onRemove);
                }
            }
            
            if (_controller.GetModel().TryGetComponent<StatusEffectRunner>(out var statusEffectRunner))
            {
                Debug.Log("Remove Effect");
                statusEffectRunner.RemoveEffect(this);
            }
        }

        public void Dispose()
        {
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
    }
}