using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public interface IEntityComponent : System.IDisposable
    {
        bool TryGetUpdate(out IObserver<float> observer);
        bool TryGetLateUpdate(out IObserver<float> observer);
        bool TryGetFixedUpdate(out IObserver<float> observer);
        
        void OnDraw(Transform origin);
        void OnDrawSelected(Transform origin);
    }
}