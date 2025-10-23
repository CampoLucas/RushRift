using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using MyTools.Global;
using UnityEngine;
using Logger = MyTools.Global.Logger;

namespace Game.Entities
{
    public class EffectInstance : IEffectInstance
    {
        private IController _controller;

        private List<IEffectStrategy> _strategies = new();
        private TriggerCollection _startTriggers = new();
        private TriggerCollection _stopTriggers = new();
        private TriggerCollection _removeTriggers = new();

        private IObserver _onStart;
        private IObserver _onStop;
        private IObserver _onRemove;
        private IObserver<float> _update;

        private bool _started;
        private bool _stopped;
        private bool _removed;
        private bool _disposed;

        private readonly bool _removeWhenApplied;
        private readonly bool _detachWhenApplied;

        private readonly float _duration;
        private readonly bool _temporary;
        private float _elapsedTime;

        public EffectInstance(IEffectStrategy[] strategies, Trigger[] startTriggers,
            Trigger[] stopTriggers, Trigger[] removeTriggers, bool removeWhenApplied, bool detachWhenApplied)
        {
            _removeWhenApplied = removeWhenApplied;
            _detachWhenApplied = detachWhenApplied;

            if (strategies is { Length: > 0 }) _strategies.AddRange(strategies);
            if (startTriggers is { Length: > 0 }) _startTriggers.AddRange(startTriggers);
            if (stopTriggers is { Length: > 0 }) _stopTriggers.AddRange(stopTriggers);
            if (removeTriggers is { Length: > 0 }) _removeTriggers.AddRange(removeTriggers);

            _onStart = new ActionObserver(OnStart);
            _onStop = new ActionObserver(OnStop);
            _onRemove = new ActionObserver(OnRemove);
        }

        public EffectInstance(IEffectStrategy[] strategies, Trigger[] startTriggers,
            Trigger[] stopTriggers, Trigger[] removeTriggers, bool removeWhenApplied, bool detachWhenApplied, float duration)
            : this(strategies, startTriggers, stopTriggers, removeTriggers, removeWhenApplied, detachWhenApplied)
        {
            if (duration <= 0) return;

            _duration = duration;
            _temporary = true;

            _update = new ActionObserver<float>(OnUpdate);
        }

        public void Initialize(IController controller)
        {
            _controller = controller;

            if (_controller == null)
            {
                // Global/no-host effect: skip runner + triggers; run immediately.
                OnStart();
                return;
            }

            if (!_controller.GetModel().TryAddOrGetComponent(StatusEffectRunnerFactory, out var statusEffectRunner))
            {
                Logger.Log("[EffectInstance] Couldn't add the component", null, LogType.Error);
                return;
            }

            statusEffectRunner.AddEffect(this);

            if (_removeTriggers.Count > 0)
            {
                // With a valid controller we can evaluate/remove immediately if needed
                if (_removeTriggers.Evaluate(ref controller))
                {
                    statusEffectRunner.RemoveEffect(this);
                    return;
                }
                _removeTriggers.Attach(_onRemove);
            }

            if (_startTriggers.Count > 0)
            {
                _startTriggers.Attach(_onStart);
                if (_startTriggers.Evaluate(ref controller))
                {
                    OnStart();
                }
            }
            else
            {
                OnStart();
            }
        }

        private StatusEffectRunner StatusEffectRunnerFactory()
        {
            return new StatusEffectRunner();
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
                _strategies[i].Dispose();

            _startTriggers.Dispose();
            _stopTriggers.Dispose();
            _removeTriggers.Dispose();
            _onStart.Dispose();
            _onStop.Dispose();
            _onRemove.Dispose();
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
            if (_disposed || (_started && _detachWhenApplied) || _removed) return;
            _started = true;
            _stopped = false;

            Start();

            if (_removeWhenApplied)
            {
                OnRemove();
                return;
            }

            if (_detachWhenApplied)
                _startTriggers.Detach(_onStart);

            _stopTriggers.Attach(_onStop);
        }

        private void OnStop()
        {
            OnStop(true);
        }

        private void OnStop(bool attachTriggers)
        {
            if (_disposed || _stopped) return;
            _stopped = true;
            _started = false;

            Stop();

            _stopTriggers.Detach(_onStop);
            if (attachTriggers) _startTriggers.Attach(_onStart);
        }

        private void OnRemove()
        {
            if (_disposed || _removed) return;
            _removed = true;

            _startTriggers.DetachAll();
            _stopTriggers.DetachAll();
            _removeTriggers.DetachAll();

            if (_started) Stop();

            // Only touch the runner if we have a controller
            if (_controller != null &&
                _controller.GetModel().TryGetComponent<StatusEffectRunner>(out var statusEffectRunner))
            {
                statusEffectRunner.RemoveEffect(this);
            }

            Dispose();
        }

        private void Start()
        {
            if (_strategies == null || _strategies.Count == 0 || _removed) return;

            for (var i = 0; i < _strategies.Count; i++)
                _strategies[i].StartEffect(_controller); // _controller may be null for global effects
        }

        private void Stop()
        {
            if (_strategies == null || _strategies.Count == 0) return;

            for (var i = 0; i < _strategies.Count; i++)
                _strategies[i].StopEffect(_controller);
        }
    }
}