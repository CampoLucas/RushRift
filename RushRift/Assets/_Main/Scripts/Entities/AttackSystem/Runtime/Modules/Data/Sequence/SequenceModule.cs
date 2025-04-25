using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace Game.Entities.AttackSystem.Modules
{
    public class SequenceModule : RuntimeModuleData
    {
        public SequenceModule(List<IModuleData> children, float duration) : base(children, duration)
        {
        }
        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return default;
            //return new SequenceProxy(this, ChildrenProxies(controller), disposeData);
        }

        public override IModuleData Clone()
        {
            return new SequenceModule(ClonedChildren(), Duration);
        }

        public override ModuleExecution GetExecution()
        {
            return ModuleExecution.Parallel;
        }

        public override bool Build(IController controller, List<IModuleData> collection, ref int index, out IModuleProxy proxy)
        {
            proxy = default;
            
            if (!TryGetOffsetData(collection, index, 1, out var data1) ||
                !TryGetOffsetData(collection, index, 2, out var data2)) return false;
            
            if (!TryGetProxy(controller, data1, data2, out proxy)) return false;

            index += 2;

            return true;
        }

        private bool TryGetProxy(IController controller, IModuleData data1, IModuleData data2, out IModuleProxy proxy)
        {
            proxy = default;
            if (!data1.CanCombineData(data2)) return false;

            var data = data1.Clone();
            data.Children.Add(data2.Clone());
            
            proxy = data.GetProxy(controller, true);
            // proxy = data1.GetProxy(controller, true);
            // proxy.AddChild(data2.GetProxy(controller, true));
            return true;
        }
    }
}