using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.DesignPatterns.Pool;
using Game.Levels;
using Game.UI.Screens.Elements;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens.Elements
{
    public sealed class LevelButton : MonoBehaviour, DesignPatterns.Observers.IObserver<ButtonSelectState>
    {
        [SerializeField] private string lockedTitle = "???";
        
        [Header("References")]
        [SerializeField] private InteractiveButton button;

        [Header("Visuals")]
        [SerializeField] private Graphic background;
        [SerializeField] private TMP_Text titleText;
        
        [Header("Medals")]
        [SerializeField] private GameObject medalsContainer;
        [SerializeField] private Graphic bronzeMedal;
        [SerializeField] private Graphic silverMedal;
        [SerializeField] private Graphic goldMedal;

        [Header("Unlocked Materials")]
        [SerializeField] private Material unlockedBackgroundMat;
        [SerializeField] private Material unlockedMedalMat;

        [Header("Locked Materials")]
        [SerializeField] private Material lockedBackgroundMat;
        [SerializeField] private Material lockedMedalMat;
        
        [Header("Animations")]
        [SerializeField] private SerializedDictionary<ButtonSelectState, UIAnimation> animations;

        private NullCheck<BaseLevelSO> _level;
        private NullCheck<UIAnimation> _runningAnim;
        private NullCheck<GameObject> _medalContainer;
        private NullCheck<Material> _unlockedBackMat;
        private NullCheck<Material> _unlockedMedalMat;
        private NullCheck<Material> _lockedBackMat;
        private NullCheck<Material> _lockedMedalMat;

        private void Awake()
        {
            _medalContainer = medalsContainer;
            _unlockedBackMat = unlockedBackgroundMat;
            _unlockedMedalMat = unlockedMedalMat;
            _lockedBackMat = lockedBackgroundMat;
            _lockedMedalMat = lockedMedalMat;
        }
        
        private void Start()
        {
            button.Attach(this);
        }

        public void Init(BaseLevelSO level, bool unlocked, int medalsUnlocked = -1)
        {
            _level = level;
            var showMedals = medalsUnlocked >= 0;

            if (!unlocked || !_level)
            {
                Lock(_lockedBackMat, _lockedMedalMat, showMedals);
            }
            else
            {
                Unlock(_level.Get().LevelName, _unlockedBackMat, _unlockedMedalMat, _lockedMedalMat, showMedals, medalsUnlocked);
            }
        }

        private void Lock(NullCheck<Material> backgroundMat, NullCheck<Material> medalMat = new(),
            bool showMedals = false)
        {
            button.enabled = false;
            titleText.text = lockedTitle;
            
            if (backgroundMat.TryGetValue(out var back))
            {
                background.material = back;
            }

            if (!_medalContainer.TryGetValue(out var container)) return;
            container.SetActive(showMedals);
            
            if (!showMedals || !medalMat.TryGetValue(out var medal)) return;
            bronzeMedal.material = medal;
            silverMedal.material = medal;
            goldMedal.material = medal;

        }

        private void Unlock(string displayName, NullCheck<Material> backgroundMat, NullCheck<Material> unlockedMedal = new(), NullCheck<Material> lockedMedal = new(),
            bool showMedals = false, int medalsUnlocked = 0)
        {
            button.enabled = true;
            titleText.text = displayName;
            
            if (backgroundMat.TryGetValue(out var back))
            {
                background.material = back;
            }

            medalsContainer.SetActive(showMedals);
            if (!showMedals || !unlockedMedal.TryGetValue(out var unlocked) || !lockedMedal.TryGetValue(out var locked)) return;

            bronzeMedal.material = medalsUnlocked > 0 ? unlocked : locked;
            silverMedal.material = medalsUnlocked > 1 ? unlocked : locked;
            goldMedal.material = medalsUnlocked > 2 ? unlocked : locked;
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
            animations = null;

            background = null;
            titleText = null;
            medalsContainer = null;
            bronzeMedal = null;
            silverMedal = null;
            goldMedal = null;
            unlockedBackgroundMat = null;
            unlockedMedalMat = null;
            lockedBackgroundMat = null;
            lockedMedalMat = null;
        }
    }

}