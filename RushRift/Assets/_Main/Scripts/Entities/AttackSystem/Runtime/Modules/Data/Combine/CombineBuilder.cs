using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    [CreateAssetMenu(menuName = "ModuleTesting/CombineBuilder")]
    public class CombineBuilder : ModuleBuilder<CombineModule>
    {
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return GetModuleData().GetProxy(controller, disposeData);
        }

        public override CombineModule GetModuleData()
        {
            return new CombineModule(Children, Duration);
        }
    }
}