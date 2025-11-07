using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.Dash
{
    public abstract class DashEffectStrategy : EffectStrategy
    {
        public sealed override void StartEffect(IController controller)
        {
            if (controller == null) return; // global call: dash needs a host, so ignore

            if (!controller.GetModel().TryGetComponent<MotionController>(out var motion))
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: The entity doesn't contain the MotionController.");
#endif
                return;
            }
            if (!motion.TryGetHandler<DashHandler>(out var dash))
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: The MotionController doesn't contain the DashHandler.");
#endif
                return;
            }
            OnStartEffect(controller.Origin, dash);
        }

        public sealed override void StopEffect(IController controller)
        {
            if (controller == null) return; // nothing to stop without a host

            if (controller.GetModel().TryGetComponent<MotionController>(out var motion) &&
                motion.TryGetHandler<DashHandler>(out var dash))
            {
                OnStopEffect(dash);
            }
        }

        protected abstract void OnStartEffect(Transform origin, DashHandler dash);
        protected abstract void OnStopEffect(DashHandler dash);
    }
}