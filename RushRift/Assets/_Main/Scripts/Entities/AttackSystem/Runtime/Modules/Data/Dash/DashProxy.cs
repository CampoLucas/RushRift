using _Main.Scripts.Entities._Player;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class DashProxy : ModuleProxy<DashModule>
    {
        private NullCheck<MotionController> _motion;
        private NullCheck<EnergyComponent> _energy;
        
        public DashProxy(DashModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (controller == null || !controller.Origin) return;
            
            if (controller.GetModel().TryGetComponent<MotionController>(out var motion)) _motion.Set(motion);
            if (controller.GetModel().TryGetComponent<EnergyComponent>(out var energy)) _energy.Set(energy);
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnStart);
        }

        private void OnStart(ModuleParams mParams)
        {
            if (!mParams.Owner)
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: The owner in DashProxy is null");
#endif
                return;
            }

            var model = mParams.Owner.Get().GetModel();
            
            
            if (!_motion)
            {
                if (model.TryGetComponent<MotionController>(out var motionComp))
                {
                    _motion.Set(motionComp);
                }
                
                if (!_motion) return;
            }

            if (!_energy)
            {
                if (model.TryGetComponent<EnergyComponent>(out var energy))
                {
                    _energy.Set(energy);
                }
            }

            var motion = _motion.Get();
            
            if (!motion.StartDash() || !_energy || !motion.TryGetHandler<DashHandler>(out var dash)) return;
            _energy.Get().Decrease(dash.GetCost());
        }
    }
}