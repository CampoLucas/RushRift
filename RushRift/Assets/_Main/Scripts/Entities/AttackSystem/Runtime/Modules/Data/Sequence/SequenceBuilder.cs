using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    [CreateAssetMenu(menuName = "ModuleTesting/CastBuilder")]
    public class SequenceBuilder : ModuleBuilder<SequenceModule>
    {
        public override SequenceModule GetModuleData()
        {
            return new SequenceModule(Children, Duration);
        }

        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return GetModuleData().GetProxy(controller, disposeData);
        }
    }
}