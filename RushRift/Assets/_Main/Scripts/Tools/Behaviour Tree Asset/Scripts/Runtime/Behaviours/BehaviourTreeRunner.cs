using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeAsset.Interfaces;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime
{
    public class BehaviourTreeRunner : MonoBehaviour
    {
        [SerializeField] private BehaviourData[] behaviours;
        //[SerializeField] private BehaviourTreeData tree;

        //private IBehaviourRunner _runner;
        private List<BehaviourTreeData> _runners;
        private Dictionary<BehaviourTreeData, IBehaviourRunner> _behaviourRunners ;
        private HashSet<BehaviourTreeData> _activeRunners;

        private void Awake()
        {
            _runners = new();
            _behaviourRunners = new();
            _activeRunners = new();
            
            // _runner = new Runner(tree.CreateBehaviour(gameObject));
            // _runner.Init();

            if (behaviours.Length == 0) return;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var container = behaviours[i];
                var behaviour = container.Behaviour;
                if (behaviour == null) continue;

                AddOrGet(behaviour, out var runner, container.StartActive);
            }
        }

        private void Update()
        {
            UpdateBehaviours();
        }
        
        public bool AddOrGet(in BehaviourTreeData behaviourData, out IBehaviourRunner runner, bool setActive = false)
        {
            if (_behaviourRunners == null || behaviourData == null)
            {
                runner = null;
                return false;
            }

            if (!_behaviourRunners.TryGetValue(behaviourData, out runner))
            {
                runner = new Runner(behaviourData.CreateBehaviour(gameObject, this));
                _behaviourRunners[behaviourData] = runner;
                _runners.Add(behaviourData);
                runner.Init();
            }

            if (setActive && _activeRunners != null && _activeRunners.Add(behaviourData))
            {
                runner.Reset();
            }
            
            return true;
        }

        public void DisableAllRunners()
        {
            _activeRunners.Clear();
        }

        public void SetRunnerActive(int index)
        {
            if (index >= _runners.Count) return;

            if (_activeRunners.Add(_runners[index]))
            {
                _behaviourRunners[_runners[index]].Reset();
            }
        }

        private void UpdateBehaviours()
        {
            var runners = _activeRunners.ToList();

            for (var i = 0; i < runners.Count; i++)
            {
                var runnerData = runners[i];
                var state = _behaviourRunners[runnerData].Update();
                if (state != NodeState.Running) _activeRunners.Remove(runnerData);
            }
        }

        private void OnDestroy()
        {
            foreach (var data in _runners)
            {
                _behaviourRunners[data].Dispose();
            }
            _behaviourRunners.Clear();
            _activeRunners.Clear();

            _behaviourRunners = null;
            _activeRunners = null;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_runners == null || _runners.Count == 0)
            {
                for (var i = 0; i < behaviours.Length; i++)
                {
                    var b = behaviours[i].Behaviour;
                    if (b == null) continue;
                    
                    DrawTree(b);
                }
            }
            else
            {
                for (var i = 0; i < _runners.Count; i++)
                {
                    DrawTree(_runners[i]);
                }
            }
#endif
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (_runners == null || _runners.Count == 0)
            {
                for (var i = 0; i < behaviours.Length; i++)
                {
                    var b = behaviours[i].Behaviour;
                    if (b == null) continue;
                    
                    DrawTreeSelected(b);
                }
            }
            else
            {
                for (var i = 0; i < _runners.Count; i++)
                {
                    DrawTreeSelected(_runners[i]);
                }
            }
#endif
        }

        private void DrawTree(BehaviourTreeData data)
        {
            if (!data) return;
            for (var i = 0; i < data.Nodes.Count; i++)
            {
                data.Nodes[i].OnDraw(transform);
            }
        }
        
        
        private void DrawTreeSelected(BehaviourTreeData data)
        {
            if (!data) return;
            for (var i = 0; i < data.Nodes.Count; i++)
            {
                data.Nodes[i].OnDrawSelected(transform);
            }
        }
    }

    [Serializable]
    public class BehaviourData
    {
        public bool StartActive => startActive;
        public BehaviourTreeData Behaviour => behaviour;

        [SerializeField] private string name;
        [SerializeField] private bool startActive;
        [SerializeField] private BehaviourTreeData behaviour;
    }
}