using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities.AttackSystem.Modules;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(fileName = "New ComboHandler", menuName = "Game/AttackSystem/ComboHandler")]
    public class Combo : ScriptableObject
    {
        [SerializeField] private List<Transition> attackTransitions;
        [SerializeField] private List<Transition> fromAnyTransitions;

        [Header("Debug")]
        [SerializeField] private Attack debugAttack;

        //[SerializeField] private PropertyCollection<StaticModuleData> defaultModules;
        
        public void OnDraw(ComboHandler comboHandler)
        {
            if (debugAttack) debugAttack.OnDraw(comboHandler);
        }

        public ComboProxy GetProxy(IController controller)
        {
            var proxy = new ComboProxy(attackTransitions, fromAnyTransitions, controller);

            // if (defaultModules != null && defaultModules.Count > 0)
            // {
            //     
            // }
            
            return proxy;
        }
    }

    public class ComboProxy : IDisposable
    {
        public List<TransitionProxy> StartTransitions { get; private set; }
        public List<TransitionProxy> FromAnyTransitions { get; private set; }

        private List<IModuleData> _data = new();
        private Dictionary<IModuleData, bool> _removableDictionary = new();
        private bool _modified;
        
        private List<IModuleProxy> _proxies = new();
        
        public ComboProxy(List<TransitionProxy> startTransitions, List<TransitionProxy> fromAnyTransitions)
        {
            StartTransitions = startTransitions;
            FromAnyTransitions = fromAnyTransitions;
        }

        public ComboProxy(List<Transition> startTransitions, List<Transition> fromAnyTransitions, IController controller) 
        {
            StartTransitions = startTransitions.Where(tr => tr != null).Select(tr => tr.GetProxy(controller)).ToList();
            FromAnyTransitions = fromAnyTransitions.Where(tr => tr != null).Select(tr => tr.GetProxy(controller)).ToList();
        }
        
        // public void SetDefaultModules()
        // {
        //     
        // }

        public void AddData(IModuleData data)
        {
            if (data == null) return;
            _data.Add(data);
            _modified = true;
        }

        public void RemoveData(IModuleData data)
        {
            if (data == null) return;
            if (!_data.Remove(data)) return;
            data.Dispose();
            _modified = true;
        }

        // when closing the inventory it would be executed
        public void BuildProxies(IController controller)
        {
            // Dispose all proxies
            for (var i = 0; i < _proxies.Count; i++)
            {
                _proxies[i].Dispose();
            }
            
            // Clear disposed proxies
            _proxies.Clear();
            
            // // Build new proxies

#if false
            for (var i = 0; i < _data.Count; i++)
            {
                if (!_data[i].Build(controller, _data, ref i, out var proxy)) continue;
                _proxies.Add(proxy);
                proxy.Init();
            }
#else
            IModuleData moduleData = null;
            
            for (var i = 0; i < _data.Count; i++)
            {
                var data = _data[i].Clone();
                
                if (moduleData == null)
                {
                    moduleData = data;
                }
                else if (moduleData.CanCombineData(data))
                {
                    var prevData = moduleData;
                    moduleData = moduleData.CombinedData(data);

                    prevData.Dispose();
                }
            }

            if (moduleData != null)
            {
                _proxies.Add(moduleData.GetProxy(controller, true));
                
            }
#endif

            

            _modified = false;
        }

        public List<IModuleProxy> GetProxies() => _proxies;
        
        public void Dispose()
        {
            
        }

        public void OnDraw(ComboHandler comboHandler)
        {
#if UNITY_EDITOR
            // var count = _proxies.Count;
            //
            // if (_proxies == null || count == 0) return;
            // for (var i = 0; i < count; i++)
            // {
            //     _proxies[i].OnDraw(comboHandler);
            // }
#endif
        }

        public void OnDrawSelected(ComboHandler comboHandler)
        {
#if UNITY_EDITOR
            // var count = _proxies.Count;
            //
            // if (_proxies == null || count == 0) return;
            // for (var i = 0; i < count; i++)
            // {
            //     _proxies[i].OnDrawSelected(comboHandler);
            // }
#endif
        }
    }
}