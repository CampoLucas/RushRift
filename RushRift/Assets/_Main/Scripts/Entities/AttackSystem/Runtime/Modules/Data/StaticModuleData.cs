using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    /// <summary>
    /// ModuleData classes that are stored as scriptable objects, cant be created at runtime
    /// </summary>
    public abstract class StaticModuleData : ScriptableObject, IModuleData
    {
        public float Duration => duration;
        public List<IModuleData> Children => children.Get<IModuleData>();
        public IModuleProxy[] ChildrenProxies(IController controller) => Children != null && Children.Count > 0
            ? Children.Select(c => c.GetProxy(controller)).ToArray()
            : Array.Empty<IModuleProxy>();

        [Header("Settings")]
        [SerializeField] private float duration;
        
        [Header("Children")]
        [SerializeField] private SerializableSOCollection<StaticModuleData> children;
        
        public abstract IModuleProxy GetProxy(IController controller, bool disposeData = false);
        public virtual ModuleExecution GetExecution() => ModuleExecution.Parallel;
        public bool Build(IController controller, List<IModuleData> collection, ref int index, out IModuleProxy proxy)
        {
            proxy = default;
            return false;
        }

        public bool CanCombineData(IModuleData data2)
        {
            return false;
        }

        public IModuleData CombinedData(IModuleData data)
        {
            return default;
        }

        public void Dispose() { }

        public virtual IModuleData Test() => default;
        public IModuleData Clone()
        {
            return this;
        }
    }
}