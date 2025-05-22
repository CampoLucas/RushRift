using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Dash
{
    public abstract class DashEffectStrategy : EffectStrategy
    {
        public sealed override void StartEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<DashComponent>(out var dash))
            {
                OnStartEffect(controller.Origin, dash);
            }
        }

        public sealed override void StopEffect(IController controller)
        {
            if (controller.GetModel().TryGetComponent<DashComponent>(out var dash))
            {
                OnStopEffect(dash);
            }
        }
        
        protected abstract void OnStartEffect( Transform origin, DashComponent dash);
        protected abstract void OnStopEffect(DashComponent dash);
    }
}