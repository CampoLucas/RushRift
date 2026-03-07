using System;
using System.Collections;
using System.Collections.Generic;
using RushRift.Environment.Interfaces;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace RushRift.Environment 
{
    public class PlatformController : ObserverComponent
    {
        public float Progress { get; private set; }
        public float IsRunning { get; private set; }
        public float IsOpening { get; private set; }
        
        [Header("Animation Settings")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool useCurve = true;
        [SerializeField, HideIf(nameof(useCurve), false)] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Animation Modules")]
        [SerializeField] private PlatformModule[] modules;

        [Header("Activation Settings")]
        [SerializeField] private bool isToggle = false;
        [SerializeField, HideIf(nameof(isToggle), true)] private bool overrideArgs = false;
        [SerializeField, HideIf(nameof(overrideArgs), false)] private string openArg, closeArg;

        private List<IPlatStartModule> _startModules = new();
        private List<IPlatUpdateModule> _updateModules = new();
        private List<IPlatEndModule> _endModules = new();

        private float _direction;

        private void Awake()
        {
            
        }

        public override void OnNotify(string arg)
        {
            throw new System.NotImplementedException();
        }
    }
}
