using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;

namespace Game.UI.Screens
{
    public class BarModel : UIModel, IObserver<float, float, float>
    {
        public AttributeBarData Data { get; private set; }
        public ISubject<float, float, float> OnValueChanged { get; private set; } = new Subject<float, float, float>();

        public BarModel(AttributeBarData data)
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
        public float StartValue => _attribute.Value;
        public float StartMaxValue => _attribute.MaxValue;
        private IAttribute _attribute;

        public AttributeBarData(IAttribute attribute, ISubject<float, float, float> onValueChanged)
        {
            _attribute = attribute;
            OnValueChanged = onValueChanged;
        }

        public void Dispose()
        {
            _attribute = null;
            OnValueChanged = null;
        }
    }
}