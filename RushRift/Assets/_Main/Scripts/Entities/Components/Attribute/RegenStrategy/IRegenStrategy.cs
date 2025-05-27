using System;

namespace Game.Entities.Components
{
    public interface IRegenStrategy<TData, TDataReturn> : IDisposable
        where TData : AttributeData<TDataReturn> where TDataReturn : IAttribute
    {
        void Tick(float delta, Attribute<TData, TDataReturn> attribute, TData data);
        void NotifyValueChanged(float oldValue, float newValue, TData data);
    }
}