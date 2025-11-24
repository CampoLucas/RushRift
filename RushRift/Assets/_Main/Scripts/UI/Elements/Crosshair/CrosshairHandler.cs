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
        [SerializeField] private Image img;
        
        [Header("Crosshair States")]
        [SerializeField] private CrosshairID root;
        [SerializeField] private SerializedDictionary<CrosshairID, CrosshairState> states;

        private Dictionary<CrosshairID, CrosshairStateInstance> _lookup = new();
        private List<CrosshairStateInstance> _instances = new();
        private CrosshairID _currentID;
        private NullCheck<CrosshairStateInstance> _current;

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

            if (!_lookup.TryGetValue(id, out var state))
            {
                this.Log($"Unknown id '{id}'", LogType.Warning);
                return;
            }

            if (_current.TryGet(out var current))
            {
                current.End();
            }

            _currentID = id;
            _current = state;
            
            state.Start();
            
            img.sprite = state.Sprite;
            var color = img.color;
            color.a = state.Alpha;
            img.color = color;
        }

        public void Stop()
        {
            Set(root);
        }
    }
}