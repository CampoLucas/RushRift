using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens.Elements
{
    public sealed class GameModeButton : MonoBehaviour, DesignPatterns.Observers.IObserver<ButtonSelectState>, ISubject
    {
        public GameModeSO Data => data;
        
        [Header("Settings")]
        [SerializeField] private GameModeSO data;
        
        [Header("Animations")]
        [SerializeField] private SerializedDictionary<ButtonSelectState, UIAnimation> animations;
        
        [Header("References")]
        [SerializeField] private InteractiveButton button;
        [SerializeField] private TMP_Text text;
        [SerializeField] private List<Graphic> icons;
        [SerializeField] private GameObject lockedVisual;

        private static readonly int Animate = Shader.PropertyToID("_Animate");
        private NullCheck<Subject> _onSelectSubject = new Subject();
        private NullCheck<UIAnimation> _runningAnim;
        private Material[] _materials;

        private void Awake()
        {
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
            button.onClick.AddListener(NotifyAll);
            button.Attach(this);
            
            
        }

        /// <summary>
        /// Initializes the button, in case its locked the given observer is disposed.
        /// </summary>
        /// <param name="onSelect"></param>
        public void Init(in IObserver onSelect)
        {
            if (Data.IsUnlocked())
            {
                Attach(onSelect);
            }
            else
            {
                onSelect.Dispose();
                button.interactable = false;
                lockedVisual.SetActive(true);
            }
            
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
            if (_runningAnim.TryGet(out var anim))
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
            if (_onSelectSubject.TryGet(out var subject))
            {
                subject.DetachAll();
                subject.Dispose();
            }

            _onSelectSubject = null;
            
            if (button)
            {
                button.onClick.RemoveListener(NotifyAll);
                button.Detach(this);
            }

            button = null;
            animations.Dispose();

            data = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public bool Attach(IObserver observer, bool disposeOnDetach = false)
        {
            return _onSelectSubject.TryGet(out var subject) && subject.Attach(observer, disposeOnDetach);
        }

        public bool Detach(IObserver observer)
        {
            return _onSelectSubject.TryGet(out var subject) && subject.Detach(observer);
        }

        public void DetachAll()
        {
            if (_onSelectSubject.TryGet(out var subject))
            {
                subject.DetachAll();
            }
        }

        public void NotifyAll()
        {
            if (_onSelectSubject.TryGet(out var subject))
            {
                subject.NotifyAll();
            }
        }
    }
}
