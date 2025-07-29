using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public class CompositeDashDirStrategy : IDashDirStrategy
    {
        private HashSet<IDashDirStrategy> _strategies = new();
        
        public Vector3 GetDir(in MotionContext context)
        {
            var dir = Vector3.zero;
            
            foreach (var strategy in _strategies)
            {
                dir += strategy.GetDir(context);
            }

            return dir.normalized;
        }

        public bool Add(IDashDirStrategy strategy) => _strategies.Add(strategy);
        public bool Remove(IDashDirStrategy strategy) => _strategies.Remove(strategy);
        
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