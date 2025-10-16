using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Inputs;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class ComboHandler : IEntityComponent
    {
        public IController Owner { get; private set; }
        public IAttack Current { get; private set; }
        public float BeginAttackTime => _timeAttackStarted;
        public List<IModuleProxy> ComboProxies => _combo ? _combo.Get().GetProxies() : null;

        private DesignPatterns.Observers.IObserver<float> _updateObserver;
        private DesignPatterns.Observers.IObserver<float> _lateUpdateObserver;
        

        private NullCheck<ComboProxy> _combo;
        private Dictionary<string, Func<bool>> _inputs;

        private float _timeAttackStarted;

        public ComboHandler(IController owner, Combo data, Dictionary<string, Func<bool>> inputs)
        {
            Owner = owner;
            SetCombo(data.GetProxy(owner));
            _inputs = inputs;
            
            _updateObserver = new ActionObserver<float>(Update);
            _lateUpdateObserver = new ActionObserver<float>(LateUpdate);
        }

        public static bool EvaluateTransitions(in IEnumerable<TransitionProxy> transitions, ComboHandler comboHandler,
            out TransitionProxy transition)
        {
            transition = null;
            var result = false;
            
            foreach (var tr in transitions)
            {
                if (tr.Evaluate(comboHandler))
                {
                    result = true;
                    transition = tr;
                    break;
                }
            }

            return result;
        }
        
        public bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = _updateObserver;
            return _updateObserver != null;
        }

        public bool TryGetLateUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = _lateUpdateObserver;
            return _lateUpdateObserver != null;
        }

        public bool TryGetFixedUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }
        
        public void Update(float delta)
        {
            if (Current == null)
            {
                if (TryGetAttack(out var transition))
                {
                    transition.Do(this);
                }
                else
                {
                    return;
                }
            }

            if (TryGetAnyTransition(out var next) || 
                Current.TryGetTransition(this, out next))
            {
                next.Do(this);
            }
            
            if (Current != null) Current.UpdateAttack(this, delta);
        }
        
        private void LateUpdate(float delta)
        {
            if (Current != null) Current.LateUpdateAttack(this, delta);
        }

        public void Dispose()
        {
            _updateObserver.Dispose();
            _updateObserver = null;
        }

        public bool GetInput(string key)
        {
            if (_inputs == null) return false;
            if (!_inputs.TryGetValue(key, out var input) || input == null) return false;

            return input();
        }

        public void SetCombo(ComboProxy data)
        {
            _combo = data;
        }

        public void SetAttack(IAttack attack)
        {
            _timeAttackStarted = Time.time;
            if (Current != null) Current.EndAttack(this);
            Current = attack;
            Current.StartAttack(this);
        }
        
        public void StopCombo()
        {
            if (Current != null) Current.EndAttack(this);
            Current = null;
        }
        
        public void SetCombo(IAttack attack)
        {
            if (attack == null) return;
            if (Current != null) Current.EndAttack(this);

            _timeAttackStarted = Time.time;
            Current = attack;
            Current.StartAttack(this);
        }
        
        public bool AttackEnded() => Current == null || Time.time - _timeAttackStarted >= Current.Duration;
        
        

        public void OnDraw(Transform origin)
        {
            if (Current != null) Current.OnDraw(origin);
        }

        public void OnDrawSelected(Transform origin)
        {
            if (Current != null) Current.OnDrawSelected(origin);
        }

        public bool Attacking()
        {
            return Current != null;
        }

        public void ForceAttack()
        {
            if (!_combo.TryGet(out var combo) || combo.StartTransitions == null || combo.StartTransitions.Count == 0) return;

            var attackTransition = _combo.Get().StartTransitions.FirstOrDefault();
            
            if (attackTransition == null) return;
            
            attackTransition.Do(this);
        }

        public void AddModule(IModuleData moduleData)
        {
            if (!_combo) return;
            
            _combo.Get().AddData(moduleData);
            _combo.Get().BuildProxies(Owner);
        }
        
        private bool TryGetAnyTransition(out TransitionProxy transition)
        {
            transition = default;
            
            return _combo &&
                   EvaluateTransitions(_combo.Get().FromAnyTransitions, this, out transition);
        }

        private bool TryGetAttack(out TransitionProxy transition)
        {
            transition = default;
            
            return _combo &&
                   EvaluateTransitions(_combo.Get().StartTransitions, this, out transition);
        }
    }
}