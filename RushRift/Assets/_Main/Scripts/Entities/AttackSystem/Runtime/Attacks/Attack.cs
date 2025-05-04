using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using Game.Entities.AttackSystem;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Attack")]
    public class Attack : ScriptableObject
    {
        public float Duration => duration;
        public bool Loop => loop;
        public SerializableSOCollection<StaticModuleData> Modules => modules;
        public List<Transition> Transitions => transitions;
        
        [Header("Settings")]
        [SerializeField] private float duration;
        [SerializeField] private bool loop;

        [Header("Modules")]
        [SerializeField] SerializableSOCollection<StaticModuleData> modules;
        
        [Header("Transitions")]
        [SerializeField] private List<Transition> transitions = new();

        public IAttack GetProxy(IController controller) => new AttackProxy(this, controller);
        
        public void OnDraw(ComboHandler comboHandler)
        {
            // if (modules is { Count: > 0 })
            // {
            //     for (var i = 0; i < modules.Count; i++)
            //     {
            //         var m = modules[i];
            //         if (m) m.Draw(comboHandler);
            //     }
            // }
            
        }
    }

    public class AttackProxy : IAttack
    {
        public float Duration => _attack.Duration;
        public bool Loop => _attack.Loop;

        private Attack _attack;

        private List<IModuleProxy> _proxies = new();
        private List<TransitionProxy> _transitions = new();
        private List<IModuleProxy> _runningProxies = new();
        
        // I use subjects so I only execute the modules start, exit and update methods only if they are used.
        private ISubject<ModuleParams> _startSubject = new Subject<ModuleParams>();
        private ISubject<ModuleParams> _endSubject = new Subject<ModuleParams>();
        private ISubject<ModuleParams, float> _updateSubject = new Subject<ModuleParams, float>();
        private ISubject<ModuleParams, float> _lateUpdateSubject = new Subject<ModuleParams, float>();
        private ModuleParams _moduleParams;

        public AttackProxy(Attack attack, IController controller)
        {
            _attack = attack;
            var modules = _attack.Modules;

            
                
            for (var i = 0; i < modules.Count; i++)
            {
                var m = modules[i];
                if (m == null) continue;

                var proxy = m.GetProxy(controller, false);
                
                
                //proxy.Initialize(this);
                AddModule(proxy);
            }

            var transitions = _attack.Transitions;
            for (var i = 0; i < transitions.Count; i++)
            {
                var tr = transitions[i];
                if (tr == null) continue;
                
                _transitions.Add(tr.GetProxy(controller));
            }
            //
            // for (var i = 0; i < _proxies.Count; i++)
            // {
            //     _proxies[i].Initialize(this);
            // }
        }
        
        public void UpdateAttack(ComboHandler comboHandler, float delta)
        {
            // If it can transition, it transitions
            if (TryGetTransition(comboHandler, out var tr))
            {
                tr.Do(comboHandler);
            }
            
            for (var i = 0; i < _runningProxies.Count; i++)
            {
                var proxy = _runningProxies[i];

                if (!proxy.Execute(_moduleParams, delta))
                {
                    _runningProxies.Remove(proxy);
                }
            }
            
            _updateSubject.NotifyAll(_moduleParams, delta);
            
            if (!Loop && ((comboHandler.BeginAttackTime + Duration) - Time.time) <= 0)
            {
                comboHandler.StopCombo();
            }
        }

        public void LateUpdateAttack(ComboHandler comboHandler, float delta)
        {
            _lateUpdateSubject.NotifyAll(_moduleParams, delta);
        }

        public bool ModulesExecuted()
        {
            return _runningProxies.Count == 0;
        }

        public void StartAttack(ComboHandler comboHandler)
        {
            _startSubject.NotifyAll(_moduleParams);
            _runningProxies.Clear();

            for (var i = 0; i < _proxies.Count; i++)
            {
                var proxy = _proxies[i];
                proxy.Reset();
                _runningProxies.Add(proxy);
            }
            
            _moduleParams = new ModuleParams()
            {
                OriginTransform = comboHandler.Owner.SpawnPos ? comboHandler.Owner.SpawnPos : comboHandler.Owner.EyesTransform,
                EyesTransform = comboHandler.Owner.EyesTransform,
                Owner = new NullCheck<IController>(comboHandler.Owner),
            };
        }

        public void EndAttack(ComboHandler comboHandler)
        {
            _endSubject.NotifyAll(_moduleParams);
            _runningProxies.Clear();
        }

        public bool TryGetTransition(ComboHandler comboHandler, out TransitionProxy transition)
        {
            return ComboHandler.EvaluateTransitions(_transitions, comboHandler, out transition);
        }

        public void Dispose()
        {
            _startSubject.Dispose();
            _startSubject = null;
            
            _endSubject.Dispose();
            _endSubject = null;
            
            _updateSubject.Dispose();
            _updateSubject = null;
            
            _lateUpdateSubject.Dispose();
            _lateUpdateSubject = null;

            for (var i = 0; i < _proxies.Count; i++)
            {
                _proxies[i].Dispose();
            }
            
            _proxies.Clear();
            _proxies = null;

            for (var i = 0; i < _transitions.Count; i++)
            {
                _transitions[i].Dispose();
            }
            
            _transitions.Clear();
            _transitions = null;

            _attack = null;
        }

        #region ModuleMethods

        public void AddModule(IModuleProxy module)
        {
            // Add module to the list of proxies.
            _proxies.Add(module);

            // Subscribe the module to the subjects.
            if (module.TryGetStart(out var start)) _startSubject.Attach(start);
            if (module.TryGetUpdate(out var update)) _updateSubject.Attach(update);
            if (module.TryGetEnd(out var end)) _endSubject.Attach(end);
            if (module.TryGetLateUpdate(out var lateUpdate)) _lateUpdateSubject.Attach(lateUpdate);
        }

        public void RemoveModule(IModuleProxy module)
        {
            _proxies.Remove(module);
            
            if (module.TryGetStart(out var start)) _startSubject.Detach(start);
            if (module.TryGetUpdate(out var update)) _updateSubject.Detach(update);
            if (module.TryGetEnd(out var end)) _endSubject.Detach(end);
            if (module.TryGetLateUpdate(out var lateUpdate)) _lateUpdateSubject.Detach(lateUpdate);

            module.Dispose();
        }

        #endregion
    }
}