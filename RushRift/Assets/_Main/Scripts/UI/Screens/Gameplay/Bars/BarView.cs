using Game.DesignPatterns.Observers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public abstract class BarView : UIView, IObserver<float, float, float>
    {
        public abstract void OnNotify(float currentHealth, float previousHealth, float maxHealth);

        public abstract void SetStartValue(float current, float max);
    }
}