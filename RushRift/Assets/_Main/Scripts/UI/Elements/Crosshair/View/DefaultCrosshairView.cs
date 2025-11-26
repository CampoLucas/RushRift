using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements.Crosshair
{
    public class DefaultCrosshairView : CrosshairView
    {
        [Header("Settings")]
        [SerializeField, Range(0, 1)] private float refillSize = 1;
        
        [Header("Visuals")]
        [SerializeField] private Image crosshairImg;
        [SerializeField] private Image refillImg;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color darkColor = Color.gray;
        [SerializeField] private Color energyColor = Color.yellow;
        
        private NullCheck<EnergyComponent> _energy;
        private NullCheck<ActionObserver<float, float, float>> _valueObserver;
        private NullCheck<ActionObserver<float>> _refillObserver;

        private Color _flashStartColor;
        private float _flashLerp;
        private bool _isFlashing;
        
        public override void Initialize(IController controller)
        {
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
            if (!_energy.TryGet(out var energy)) return;
            
            UpdateState(energy);
            AttachObservers(energy);
        }

        protected override void OnHide()
        {
            if (!_energy.TryGet(out var energy)) return;
            
            DetachObservers(energy);
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _flashLerp += Time.unscaledDeltaTime * 6f;
            var t = Mathf.Clamp01(_flashLerp);

            crosshairImg.color = Color.Lerp(_flashStartColor, defaultColor, t);

            if (t >= 1f) _isFlashing = false;
        }

        private void UpdateState(in EnergyComponent energy)
        {
            var current = energy.Value;
            var max = energy.MaxValue;

            // refill bar starts hidden unless refilling
            refillImg.gameObject.SetActive(energy.IsRefilling);
            refillImg.color = defaultColor;

            // refill bar should reflect a QUARTER circle
            refillImg.fillAmount = energy.RefillProgress * refillSize;

            // Color logic
            if (current <= 0)
            {
                crosshairImg.color = darkColor;
                return;
            }

            if (current >= max)
            {
                crosshairImg.color = energyColor;
                return;
            }

            // default if none of the above
            crosshairImg.color = defaultColor;
        }

        private void AttachObservers(in EnergyComponent energy)
        {
            if (_valueObserver.TryGet(out var valueObs, GetValueObserver))
            {
                energy.OnValueChanged.Attach(valueObs);
            }

            if (_refillObserver.TryGet(out var refillObs, GetRefillObserver))
            {
                energy.OnRefill.Attach(refillObs);
            }
        }

        private void DetachObservers(in EnergyComponent energy)
        {
            if (_valueObserver.TryGet(out var valueObs))
            {
                energy.OnValueChanged.Detach(valueObs);
            }

            if (_refillObserver.TryGet(out var refillObs))
            {
                energy.OnRefill.Detach(refillObs);
            }
        }

        private ActionObserver<float, float, float> GetValueObserver()
        {
            return new ActionObserver<float, float, float>(OnValueChangedHandler);
        }

        private ActionObserver<float> GetRefillObserver()
        {
            return new ActionObserver<float>(OnRefillHandler);
        }

        private void OnValueChangedHandler(float current, float previous, float max)
        {
            // hit 0 => dark
            if (current <= 0f)
            {
                crosshairImg.color = darkColor;
                return;
            }

            // refill started?
            if (current == 0 && previous > 0)
            {
                crosshairImg.color = darkColor;
            }

            // flash yellow when energy increases
            if (current > previous)
            {
                StartFlash(energyColor);
            }

            if (current < previous)
            {
                StartFlash(darkColor);
            }

            // max energy => yellow solid
            if (current >= max)
            {
                _isFlashing = false;
                crosshairImg.color = energyColor;
            }
            else
            {
                crosshairImg.color = defaultColor;
            }
        }

        private void OnRefillHandler(float amount)
        {
            if (!_energy.TryGet(out var energy)) return;
            
            // show/hide refill arc
            refillImg.gameObject.SetActive(energy.IsRefilling);

            // quarter circle visual
            refillImg.fillAmount = amount * refillSize;
        }
        
        private void StartFlash(Color color)
        {
            _flashStartColor = color;
            _flashLerp = 0f;
            _isFlashing = true;
            crosshairImg.color = color;
        }
    }
}