using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class CompositeDashEndStrategy : IDashEndStrategy
    {
        private HashSet<IDashEndStrategy> _strategies = new();
        
        public void OnDashEnd(in MotionContext context)
        {
            foreach (var strategy in _strategies)
            {
                strategy.OnDashEnd(context);
            }
        }

        public bool Add(IDashEndStrategy strategy) => _strategies.Add(strategy);
        public bool Remove(IDashEndStrategy strategy) => _strategies.Remove(strategy);
        
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