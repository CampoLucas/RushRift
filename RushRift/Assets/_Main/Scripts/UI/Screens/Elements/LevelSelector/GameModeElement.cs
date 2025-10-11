using System;
using System.Collections.Generic;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens.Elements
{
    public class GameModeElement : MonoBehaviour, DesignPatterns.Observers.IObserver<ButtonSelectState>
    {
        [Header("Animations")]
        [SerializeField] private SerializedDictionary<ButtonSelectState, UIAnimation> animations;
        
        [Header("References")]
        [SerializeField] private InteractiveButton button;
        [SerializeField] private TMP_Text text;
        [SerializeField] private List<Graphic> icons;

        private NullCheck<UIAnimation> _runningAnim;
        private Material[] _materials;
        private static readonly int Animate = Shader.PropertyToID("_Animate");

        private void Awake()
        {
            if (button) button.onClick.AddListener(() => { this.Log("Press button"); });

            _materials = new Material[icons.Count];
            
            if (icons != null && icons.Count > 0)
            {
                for (var i = 0; i < icons.Count; i++)
                {
                    var icon = icons[i];
                    if (!icon) continue;
                    
                    var material = new Material(icon.material);
                    icon.material = material;

                    _materials[i] = material;
                }
            }
        }

        private void Start()
        {
            button.Attach(this);
            
            AnimateIcon(false);
        }

        public void AnimateIcon(bool value)
        {
            if (_materials == null || _materials.Length == 0) return;
            
            foreach (var material in _materials)
            {
                material.SetFloat(Animate, value ? 1 : 0);
            }
        }

        public void OnNotify(ButtonSelectState state)
        {
            if (_runningAnim.TryGetValue(out var anim))
            {
                anim.Stop();
            }
            
            if (animations.TryGetValue(state, out var animator))
            {
                _runningAnim = animator;
                animator.Play();
            }
        }

        public void Dispose()
        {
            if (button)
            {
                button.Detach(this);
            }

            button = null;
            animations.Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
