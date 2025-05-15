using Game.DesignPatterns.Observers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class AttributeBarView : UIView, IObserver<float, float, float>
    {
        [SerializeField] private Image healthBarFill;
        [SerializeField] private TMP_Text text;

        public void SetValue(float currentValue, float maxValue)
        {
            healthBarFill.fillAmount = currentValue / maxValue;
            text.text = $"{(int)currentValue}/{maxValue}";
        }
        
        public void OnNotify(float currentHealth, float previousHealth, float maxHealth)
        {
            SetValue(currentHealth, maxHealth);
            // healthBarFill.fillAmount = arg.Item1 / arg.Item3;
            // text.text = $"{arg.Item1}/{arg.Item3}";
        }

        public override void Dispose()
        {
            base.Dispose();
            healthBarFill = null;
            text = null;
        }
    }
}