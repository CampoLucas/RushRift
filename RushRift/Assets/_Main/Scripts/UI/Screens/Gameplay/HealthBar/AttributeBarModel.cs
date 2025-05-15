using System;
using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class AttributeBarModel : UIModel, IObserver<float, float, float>
    {
        public AttributeBarData Data { get; private set; }
        public ISubject<float, float, float> OnValueChanged { get; private set; } = new Subject<float, float, float>();

        public AttributeBarModel(AttributeBarData data)
        {
            Data = data;
            Data.OnValueChanged.Attach(this);
        }

        public void OnNotify(float currentHealth, float previousHealth, float maxHealth)
        {
            OnValueChanged.NotifyAll(currentHealth, previousHealth, maxHealth);
        }

        public override void Dispose()
        {
            base.Dispose();
            Data.OnValueChanged.Detach(this);
            Data.Dispose();
            
            OnValueChanged.DetachAll();
            OnValueChanged.Dispose();
            OnValueChanged = null;
        }
    }

    public struct AttributeBarData : IDisposable
    {
        public ISubject<float, float, float> OnValueChanged { get; private set; }
        public float StartValue { get; private set; }
        public float StartMaxValue { get; private set; }

        public AttributeBarData(float startValue, float startMaxValue, ISubject<float, float, float> onValueChanged)
        {
            StartValue = startValue;
            StartMaxValue = startMaxValue;
            OnValueChanged = onValueChanged;
        }

        public void Dispose()
        {
            OnValueChanged = null;
        }
    }
}