using System;
using System.Collections.Generic;
using MyTools.Global;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements.Crosshair
{
    [DisallowMultipleComponent]
    public class CrosshairHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform viewRoot;
        
        [Header("Crosshair States")]
        [SerializeField] private CrosshairID root;
        [SerializeField] private SerializedDictionary<CrosshairID, CrosshairState> states;

        private Dictionary<CrosshairID, CrosshairStateInstance> _lookup = new();
        private List<CrosshairStateInstance> _instances = new();
        private CrosshairID _currentID;
        private NullCheck<CrosshairStateInstance> _current;
        private NullCheck<CrosshairView> _activeView;

        private void Start()
        {
            if (!PlayerSpawner.Player.TryGet(out var player)) return;
            
            foreach (var key in states.Keys)
            {
                var s = states[key];
                if (!s) continue;
                
                var inst = s.CreateInstance(player);
                inst.OnStartRequested += () => Set(key);
                inst.OnStopRequested += () => Stop();
                
                if (!_lookup.TryAdd(key, inst))
                {
                    inst.Dispose();
                    continue;
                }
                
                _instances.Add(inst);
            }
            
            Set(root);
        }

        public void Set(CrosshairID id)
        {
            if (_current && _currentID == id)
            {
                return;
            }

            if (!_lookup.TryGetValue(id, out var newState))
            {
                this.Log($"Unknown id '{id}'", LogType.Warning);
                return;
            }

            // End previous state
            if (_current.TryGet(out var prevState))
            {
                prevState.End();
                
                // Hide previous view
                if (_activeView.TryGet(out var prevView))
                {
                    prevView.Hide();
                }
            }

            _currentID = id;
            _current = newState;
            
            // Start new state
            newState.Start();
            
            // Get or create the view instance for this state
            var view = newState.GetView(viewRoot, PlayerSpawner.Player.Get());
            if (view != null)
            {
                view.Show();
                _activeView = view;
            }
            else
            {
                _activeView = null;
            }
        }

        public void Stop()
        {
            Set(root);
        }
        
        private void OnDestroy()
        {
            // End current state safely
            if (_current.TryGet(out var currentState))
            {
                currentState.End();
            }

            // Hide and destroy active view
            if (_activeView.TryGet(out var active))
            {
                active.Hide();
                Destroy(active.gameObject);
                _activeView = null;
            }

            // Dispose all state instances
            foreach (var inst in _instances)
            {
                if (inst != null)
                {
                    // Remove trigger listeners
                    inst.OnStartRequested = null;
                    inst.OnStopRequested = null;

                    inst.Dispose();

                    // Destroy their views if they were created
                    var view = inst.GetView(viewRoot, PlayerSpawner.Player.Get());
                    if (view != null)
                    {
                        view.Hide();
                        Destroy(view.gameObject);
                    }
                }
            }

            _lookup.Clear();
            _instances.Clear();
        }
    }
}