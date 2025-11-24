using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Saves;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements.Crosshair
{
    public class BlinkCrosshairView : CrosshairView
    {
        [SerializeField] private Image chargeImg;
        [SerializeField] private Color chargeColor = Color.magenta;
        [SerializeField] private bool setColorFromSettings;

        private NullCheck<ActionObserver<float>> _onChargeObserver;
        private NullCheck<Image> _chargeImg;
        
        public override void Initialize(IController controller)
        {
            _chargeImg = chargeImg;
        }

        protected override void OnShow()
        {
            if (!_chargeImg.TryGet(out var image)) return;

            image.color = setColorFromSettings ? SettingsData.GetBlinkChargeColor() : chargeColor;
            
            image.fillAmount = 0;
            if (_onChargeObserver.TryGet(out var observer, () => new ActionObserver<float>(OnChargeHandler)))
            {
                LockOnBlink.ChargeAmount.Attach(observer);
            }
        }

        protected override void OnHide()
        {
            if (_onChargeObserver.TryGet(out var observer))
            {
                LockOnBlink.ChargeAmount.Detach(observer);
            }
        }

        private void OnChargeHandler(float t)
        {
            if (!_chargeImg.TryGet(out var image)) return;
            image.fillAmount = t;
        }

        private void OnDestroy()
        {
            chargeImg = null;
            
            if (_onChargeObserver.TryGet(out var observer))
            {
                LockOnBlink.ChargeAmount.Detach(observer);
            }
            
            _onChargeObserver.Dispose();
            _chargeImg.Dispose();
        }
    }
}