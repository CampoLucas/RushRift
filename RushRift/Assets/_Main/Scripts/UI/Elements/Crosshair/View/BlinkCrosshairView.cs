using System;
using System.Linq;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Levels;
using Game.Saves;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements.Crosshair
{
    public class BlinkCrosshairView : CrosshairView
    {
        [SerializeField] private Image crosshairImg;
        [SerializeField] private Image chargeImg;
        [SerializeField] private Color chargeColor = Color.magenta;
        [SerializeField] private bool setColorFromSettings;
        
        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color darkColor = Color.gray;
        [SerializeField] private Color energyColor = Color.yellow;

        [Header("Reference")]
        [SerializeField] private GameObject distanceObj;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private UIAnimationRunner tutorial;

        private NullCheck<EnergyComponent> _energy;
        private NullCheck<ActionObserver<float, float, float>> _valueObserver;
        private NullCheck<ActionObserver<float>> _onChargeObserver;
        private NullCheck<Image> _chargeImg;

        private bool _played;
        private bool _showTutorial;
        
        public override void Initialize(IController controller)
        {
            _chargeImg = chargeImg;
            
            if (controller == null) return;
            
            var model = controller.GetModel();
            if (model == null) return;

            if (model.TryGetComponent<EnergyComponent>(out var energy))
            {
                _energy = energy;
            }
        }

        protected override void OnShow()
        {
            if (!_chargeImg.TryGet(out var image)) return;
            _played = false;
            
            image.color = setColorFromSettings ? SettingsData.GetBlinkChargeColor() : chargeColor;
            image.fillAmount = 0;
            
            var hasEnergy = _energy.TryGet(out var energy);
            if (hasEnergy)
            {
                UpdateState(energy.Value, energy.MaxValue);
            }
            else
            {
                this.Log("Crosshair doens't have energy", LogType.Error);
            }
            
            if (hasEnergy && _valueObserver.TryGet(out var valueObs, GetValueObserver))
            {
                energy.OnValueChanged.Attach(valueObs);
            }

            if (!PlayerSpawner.Player.TryGet(out var player) ||
                !player.GetModel().TryGetComponent<BlinkComponent>(out var blink) ||
                !blink.ProgressUpdated.TryGet(out var subject) ||
                !_onChargeObserver.TryGet(out var observer, () => new ActionObserver<float>(OnChargeHandler))) return;
            
            subject.Attach(observer);
                
            // Show the key tutorial
            _showTutorial = blink.ExecutedCount() < 1 && 
                            tutorial &&
                            GlobalLevelManager.CurrentLevel.TryGet(out var levelSo) &&
                            levelSo.HasArgument(LevelArgument.BlinkTutorial);
            if (_showTutorial && hasEnergy && !_energy.Get().IsEmpty())
            {
                ShowTutorial();
            }

        }

        protected override void OnHide()
        {
            if (_onChargeObserver.TryGet(out var observer) && 
                PlayerSpawner.Player.TryGet(out var player) && 
                player.GetModel().TryGetComponent<BlinkComponent>(out var blink) &&
                blink.ProgressUpdated.TryGet(out var subject))
            {
                subject.Detach(observer);
            }
            
            if (_energy.TryGet(out var energy) && _valueObserver.TryGet(out var valueObs, GetValueObserver))
            {
                energy.OnValueChanged.Detach(valueObs);
            }


            if (tutorial && tutorial.isActiveAndEnabled)
            {
                HideTutorial();
            } 
        }

        private void ShowTutorial()
        {
            if (_played || !_showTutorial) return;
            
            this.Log("Show tutorial", LogType.Error);
            _played = true;
            tutorial.gameObject.SetActive(true);
            tutorial.Play();
            
        }

        private void HideTutorial()
        {
            tutorial.Stop();
            tutorial.gameObject.SetActive(false);
        }

        private void OnChargeHandler(float t)
        {
            if (!_chargeImg.TryGet(out var image)) return;
            image.fillAmount = t;
        }
        
        private void OnValueChangedHandler(float current, float previous, float max)
        {
            UpdateState(current, max);
        }
        
        private ActionObserver<float, float, float> GetValueObserver()
        {
            return new ActionObserver<float, float, float>(OnValueChangedHandler);
        }
        
        private void UpdateState(float current, float max)
        {
            crosshairImg.color = current > 0 
                ? current >= max ? energyColor : defaultColor 
                : darkColor;

            if (current > 1)
            {
                if (distanceObj.activeSelf == false) distanceObj.SetActive(true);

                distanceText.text = $"+{(int)current - 1}";
            }
            else if (distanceObj.activeSelf)
            {
                 distanceObj.SetActive(false);
            }

            if (_showTutorial && current > 0)
            {
                ShowTutorial();
            }

            if (_showTutorial && current == 0)
            {
                HideTutorial();
            }
        }

        private void OnDestroy()
        {
            chargeImg = null;
            
            if (_onChargeObserver.TryGet(out var observer) && 
                PlayerSpawner.Player.TryGet(out var player) && 
                player.GetModel().TryGetComponent<BlinkComponent>(out var blink) &&
                blink.ProgressUpdated.TryGet(out var subject))
            {
                subject.Detach(observer);
            }
            
            _onChargeObserver.Dispose();
            _chargeImg.Dispose();
        }
    }
}