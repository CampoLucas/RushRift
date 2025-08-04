using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashUpdateStrategyComposite : IDashUpdateStrategy
    {
        private List<DashUpdateEnum> _strategies = new();
        private Dictionary<DashUpdateEnum,IDashUpdateStrategy> _strategiesDict = new();

        public void OnReset()
        {
            for (var i = 0; i < _strategies.Count; i++)
            {
                var s = _strategiesDict[_strategies[i]];
                if (s == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"WARNING: Null strategy in {nameof(DashUpdateStrategyComposite)}'s OnReset method.");    
#endif
                    continue;
                }

                s.OnReset();
            }
        }

        public bool OnDashUpdate(in MotionContext context, in float delta)
        {
            Debug.Log($"SuperTest: strategies {_strategies.Count}");
            
            for (var i = 0; i < _strategies.Count; i++)
            {
                var s = _strategiesDict[_strategies[i]];
                if (s == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"WARNING: Null strategy in {nameof(DashUpdateStrategyComposite)}'s OnReset method.");    
#endif
                    continue;
                }

                if (s.OnDashUpdate(context, delta))
                {
                    return true; // true means that the dash is over
                }
            }

            return false;
        }

        public bool Add(DashUpdateEnum id, IDashUpdateStrategy strategy)
        {
            if (_strategiesDict.ContainsKey(id))
            {
#if UNITY_EDITOR
                Debug.Log($"WARNING: Already contains the {id.DisplayName()} strategy");
#endif
                return false;
            }    
            
            _strategies.Add(id);
            _strategiesDict[id] = strategy;
            return true;
        }

        public bool Remove(DashUpdateEnum id)
        {
            if (!_strategiesDict.Remove(id, out var strategy))
            {
                return false;
            }

            _strategies.Remove(id);
            strategy.Dispose();
            return true;
        }
        public void Dispose()
        {
            foreach (var strategy in _strategies)
            {
                _strategiesDict[strategy].Dispose();
            }
            
            _strategies.Clear();
            _strategies = null;
            
            _strategiesDict.Clear();
            _strategiesDict = null;
        }
    }
}