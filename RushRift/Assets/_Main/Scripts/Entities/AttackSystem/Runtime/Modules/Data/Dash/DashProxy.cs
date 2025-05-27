using _Main.Scripts.Entities._Player;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class DashProxy : ModuleProxy<DashModule>
    {
#if false
        private NullCheck<DashAbility> _dash;
        
        public DashProxy(DashModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (_dash || controller == null || !controller.Origin || !controller.Origin.gameObject.TryGetComponent<DashAbility>(out var dash)) return;
            
            _dash.Set(dash);
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnStart);
        }

        private void OnStart(ModuleParams mParams)
        {
            if (!_dash)
            {
                if (mParams.OriginTransform.gameObject.TryGetComponent<DashAbility>(out var dash))
                {
                    _dash.Set(dash);
                }
                
                
                if (!_dash) return;
            }
            
            Debug.Log("Do dash");
            _dash.Get().StartDash();
        }
#else
        private NullCheck<DashComponent> _dash;
        private NullCheck<EnergyComponent> _energy;
        
        public DashProxy(DashModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (_dash || controller == null || !controller.Origin) return;
            
            if (controller.GetModel().TryGetComponent<DashComponent>(out var dash)) _dash.Set(dash);
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
            
            
            if (!_dash)
            {
                if (model.TryGetComponent<DashComponent>(out var dash))
                {
                    _dash.Set(dash);
                }
                
                if (!_dash) return;
            }

            if (!_energy)
            {
                if (model.TryGetComponent<EnergyComponent>(out var energy))
                {
                    _energy.Set(energy);
                }
            }
            
            Debug.Log("Do dash");
            if (_dash.Get().StartDash() && _energy) _energy.Get().Decrease(_dash.Get().Cost);
        }
#endif
    }
}