using System;
using System.Collections.Generic;
using Game.DesignPatterns.Factory;

namespace Game.Entities.AttackSystem.Modules
{
    public interface IModuleData : IDisposable, IPrototype<IModuleData>
    {
        float Duration { get; }
        List<IModuleData> Children { get; }

        /// <summary>
        /// Creates a ModuleProxy
        /// </summary>
        /// <returns></returns>
        IModuleProxy GetProxy(IController controller, bool disposeData = false);
        ModuleExecution GetExecution();
        bool Build(IController controller, List<IModuleData> collection, ref int index, out IModuleProxy proxy);
        bool CanCombineData(IModuleData data2);
        IModuleData CombinedData(IModuleData data);
    }
}