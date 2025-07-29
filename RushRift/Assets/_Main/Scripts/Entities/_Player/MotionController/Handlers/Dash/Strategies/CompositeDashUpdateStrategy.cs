using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class CompositeDashUpdateStrategy : IDashUpdateStrategy
    {
        private HashSet<IDashUpdateStrategy> _strategies = new();
        
        public bool OnDashUpdate(in MotionContext context, in float delta)
        {
            foreach (var strategy in _strategies)
            {
                if (strategy.OnDashUpdate(context, delta))
                {
                    return true; // true means that the dash is over
                }
            }

            return false;
        }

        public bool Add(IDashUpdateStrategy strategy) => _strategies.Add(strategy);
        public bool Remove(IDashUpdateStrategy strategy) => _strategies.Remove(strategy);
        
        public void Dispose()
        {
            foreach (var strategy in _strategies)
            {
                strategy.Dispose();
            }
            
            _strategies.Clear();
            _strategies = null;
        }
    }
}