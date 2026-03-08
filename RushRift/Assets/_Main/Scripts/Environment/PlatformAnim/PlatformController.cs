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
        [SerializeField] private bool instant;
        [SerializeField, ReadOnlyIf(nameof(instant), true)] private float duration = 1f;
        [SerializeField, ReadOnlyIf(nameof(instant), true)] private bool useCurve = true;
        [SerializeField, ReadOnlyIf(nameof(instant), true), HideIf(nameof(useCurve), false)] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Animation Modules")]
        [SerializeField] private PlatformModule[] modules;

        [Header("Activation Settings")]
        [SerializeField] private bool invertStartDir = false;
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
                modules[i]?.Initialize(this);
            }

            if (startProgress <= 0)
            {
                invertStartDir = true;
            }
            else if (startProgress >= 1)
            {
                invertStartDir = false;
            }
            
        }

        private void Start()
        {
            if (startProgress >= 0)
            {
                StartPlatform(false, startProgress, direction: invertStartDir);
            }
        }

        private void Update()
        {
            UpdatePlatform(Time.deltaTime);
        }

        public void Open()
        {
            StartPlatform(false, isInstant: instant, direction: !IsInverse);
        }

        public void Close()
        {
            StartPlatform(true, isInstant: instant, direction: !IsInverse);
        }

        private void StartPlatform(bool inverse, float sProgress = -1, bool isInstant = false, bool direction = false)
        {
            IsInverse = direction;
            _direction = direction ? -1f : 1f;

            if (sProgress >= 0)
            {
                Progress = Mathf.Clamp01(sProgress);
            }

            // Trigger Start Modules
            for (var i = 0; i < _startModules.Count; i++)
            {
                _startModules[i]?.OnStart(inverse);
            }

            if (isInstant)
            {
                Progress = direction ? 0f : 1f; // Set to final state immediately
                Finish();
                return;
            }
            
            IsRunning = true;
        }

        private void UpdatePlatform(float delta)
        {
            if (!IsRunning || instant) return;

            var rawProgress = Progress + (_direction * (delta / duration));
            Progress = Mathf.Clamp01(rawProgress);
            
            var evaluatedProgress = useCurve && curve != null ? curve.Evaluate(Progress) : Progress;
            
            for (var i = 0; i < _updateModules.Count; i++)
            {
                _updateModules[i]?.OnUpdate(evaluatedProgress, IsInverse, delta);
            }

            var finished = (IsInverse && Progress <= 0f) || (!IsInverse && Progress >= 1f);

            if (finished)
            {
                Finish();
            }
        }

        private void Finish()
        {
            IsRunning = false;
            
            // Ensure update modules get the absolute final value (0 or 1)
            var finalValue = IsInverse ? 0f : 1f;
            for (var i = 0; i < _updateModules.Count; i++)
            {
                _updateModules[i]?.OnUpdate(finalValue, IsInverse, 0);
            }

            for (var i = 0; i < _endModules.Count; i++)
            {
                _endModules[i]?.OnEnd(IsInverse);
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
                else if (arg == CloseArg())
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
        
        private void OnDestroy()
        {
            _startModules?.Clear();
            _updateModules?.Clear();
            _endModules?.Clear();
            
            _startModules = null;
            _updateModules = null;
            _endModules = null;

            modules = null;
        }
    }
}
