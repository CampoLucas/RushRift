using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.LevelElements;
using MyTools.Global;
using RushRift.Environment.Interfaces;
using Tools.Scripts.PropertyAttributes;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace RushRift.Environment 
{
    public class PlatformController : ObserverComponent
    {
        public float Progress { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsInverse { get; private set; }
        
        [Header("Animation Settings")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool useCurve = true;
        [SerializeField, HideIf(nameof(useCurve), false)] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Animation Modules")]
        [SerializeField] private PlatformModule[] modules;

        [Header("Activation Settings")]
        [SerializeField] private float startProgress = 0;
        [SerializeField] private bool isToggle = false;
        [SerializeField, HideIf(nameof(isToggle), true)] private bool overrideArgs = false;
        [SerializeField, HideIf(nameof(overrideArgs), false)] private string openArg, closeArg;

        private List<IPlatStartModule> _startModules = new();
        private List<IPlatUpdateModule> _updateModules = new();
        private List<IPlatEndModule> _endModules = new();

        private float _direction;

        
        
        private void Awake()
        {
            if (modules == null || modules.Length == 0) return;
            _startModules.AddRange(modules.OfType<IPlatStartModule>());
            _updateModules.AddRange(modules.OfType<IPlatUpdateModule>());
            _endModules.AddRange(modules.OfType<IPlatEndModule>());

            for (var i = 0; i < modules.Length; i++)
            {
                modules[i].Initialize(this);
            }
            
        }

        private void Start()
        {
            if (startProgress >= 0)
            {
                StartPlatform(false, startProgress);
            }
        }

        private void Update()
        {
            UpdatePlatform(Time.deltaTime);
        }

        public void Open()
        {
            StartPlatform(false);
        }

        public void Close()
        {
            StartPlatform(true);
        }

        private void StartPlatform(bool inverse, float startProgress = -1)
        {
            IsInverse = inverse;
            _direction = inverse ? -1f : 1f;
            IsRunning = true;

            if (startProgress >= 0)
            {
                Progress = Mathf.Clamp01(startProgress);
            }

            for (var i = 0; i < _startModules.Count; i++)
            {
                var m = _startModules[i];
                if (m == null)
                {
                    this.Log("Trying to call a null start module.", LogType.Warning);
                    continue;
                }
                
                m.OnStart(inverse);
            }
        }

        private void UpdatePlatform(float delta)
        {
            if (!IsRunning) return;

            Progress += _direction * (delta / duration);
            Progress = Mathf.Clamp01(Progress);

            for (var i = 0; i < _updateModules.Count; i++)
            {
                var m = _updateModules[i];
                if (m == null)
                {
                    this.Log("Trying to call a null update module.", LogType.Warning);
                    continue;
                }
                
                m.OnUpdate(Progress, IsInverse, delta);
            }

            var finished = (IsInverse && Progress >= 1f) || (!IsInverse && Progress <= 0f);

            if (finished)
            {
                IsRunning = false;

                for (var i = 0; i < _endModules.Count; i++)
                {
                    var m = _endModules[i];
                    if (m == null)
                    {
                        this.Log("Trying to call a null end module.", LogType.Warning);
                        continue;
                    }
                    
                    m.OnEnd(IsInverse);
                }
            }
        }

        public override void OnNotify(string arg)
        {
            if (!isToggle)
            {
                if (arg == OpenArg())
                {
                    Open();
                }
                else
                {
                    Close();
                }
                
                return;
            }

            if (IsInverse)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        private string OpenArg() => overrideArgs ? openArg : Terminal.ON_ARGUMENT;
        private string CloseArg() => overrideArgs ? closeArg : Terminal.OFF_ARGUMENT;
    }
}
