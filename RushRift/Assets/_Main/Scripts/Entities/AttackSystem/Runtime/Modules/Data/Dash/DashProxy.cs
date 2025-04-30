using _Main.Scripts.Entities._Player;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class DashProxy : ModuleProxy<DashModule>
    {
        private NullCheck<DashAbility> _dash;
        
        public DashProxy(DashModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (_dash || controller == null || !controller.Transform || !controller.Transform.gameObject.TryGetComponent<DashAbility>(out var dash)) return;
            
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
    }
}