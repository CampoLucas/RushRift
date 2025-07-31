using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class DashDirStrategyComposite : IDashDirStrategy
    {
        private List<DashDirEnum> _strategies = new();
        private Dictionary<DashDirEnum,IDashDirStrategy> _strategiesDict = new();
        
        public Vector3 GetDir(in MotionContext context, in DashConfig config)
        {
            var dir = Vector3.zero;

            for (var i = 0; i < _strategies.Count; i++)
            {
                var s = _strategiesDict[_strategies[i]];
                if (s == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"WARNING: Null strategy in {nameof(DashDirStrategyComposite)}'s GetDir method.");    
#endif
                    continue;
                }

                dir += s.GetDir(context, config);
            }

            return dir.normalized;
        }

        public bool Add(DashDirEnum id, IDashDirStrategy strategy)
        {
            if (_strategiesDict.ContainsKey(id))
            {
                return false;
            }    
            
            _strategies.Add(id);
            _strategiesDict[id] = strategy;
            return true;
        }

        public bool Remove(DashDirEnum id)
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