using System.Collections.Generic;

namespace Game.Entities.AttackSystem.Modules
{
    public class CombineModule : RuntimeModuleData
    {
        public CombineModule(List<IModuleData> children, float duration) : base(children, duration)
        {
        }

        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return default;
        }

        public override IModuleData Clone()
        {
            return new CombineModule(ClonedChildren(), Duration);
        }

        public override bool Build(IController controller, List<IModuleData> collection, ref int index, out IModuleProxy proxy)
        {
            proxy = default;

            if (!TryGetOffsetData(collection, index, 1, out var data1) ||
                !TryGetOffsetData(collection, index, 2, out var data2)) return false;

            if (!TryGetCombinedProxy(controller, data1, data2, out proxy)) return false;

            index += 2;

            return true;
        }

        private bool TryGetCombinedProxy(IController controller, IModuleData data1, IModuleData data2, out IModuleProxy proxy)
        {
            proxy = default;
            if (!data1.CanCombineData(data2)) return false;

            proxy = data1.CombinedData(data2).GetProxy(controller, true);
            return true;
        }

        
    }
}