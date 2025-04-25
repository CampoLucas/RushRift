using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    /// <summary>
    /// ModulesData that are created in runtime
    /// </summary>
    public abstract class RuntimeModuleData : IModuleData
    {
        public float Duration { get; }
        public List<IModuleData> Children { get; private set; } = new List<IModuleData>();

        protected RuntimeModuleData(List<IModuleData> children, float duration)
        {
            if (children != null && children.Count > 0)
            {
                Children = children;
            }
            Duration = duration;
        }
        
        public abstract IModuleProxy GetProxy(IController controller, bool disposeData = false);
        public abstract IModuleData Clone();
        public virtual ModuleExecution GetExecution() => ModuleExecution.Parallel;
        public virtual bool Build(IController controller, List<IModuleData> collection, ref int index, out IModuleProxy proxy)
        {
            proxy = GetProxy(controller);
            return proxy != null;
        }

        public void Dispose()
        {
            OnDispose();
            if (Children != null) Children.Clear();
            Children = null;
        }
        
        protected virtual void OnDispose() { }
        
        protected bool TryGetOffsetData(List<IModuleData> collection, int index, int i, out IModuleData moduleData)
        {
            moduleData = default;
            var offset = index + i;
            var diff = (offset) - (collection.Count - 1);
            
            var target = diff < 1 ? offset : diff - 1;

            if (target == index || target >= collection.Count) return false;
            moduleData = collection[target];
            return true;
        }
        
        public virtual bool CanCombineData(IModuleData data2)
        {
            return false;
        }
        
        public virtual IModuleData CombinedData(IModuleData data)
        {
            return default;
        }
        
        // protected IModuleProxy[] ChildrenProxies(IEntityController controller) => Children != null && Children.Count > 0
        //     ? Children.Select(c => c.GetProxy(controller)).ToArray()
        //     : Array.Empty<IModuleProxy>();
        protected IModuleProxy[] ChildrenProxies(IController controller)
        {
            if (Children == null || Children.Count == 0)
            {
#if UNITY_EDITOR
                Debug.Log("Module didn't have children");
#endif
                return Array.Empty<IModuleProxy>();
            }

            var childrenList = new List<IModuleProxy>();
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child == null)
                {
#if UNITY_EDITOR
                    Debug.Log("Child is null.");
#endif
                    continue;
                }

                var proxy = child.GetProxy(controller);
                if (proxy == null)
                {
#if UNITY_EDITOR
                    Debug.Log("Child proxy is null.");
#endif
                    continue;
                }
                
                childrenList.Add(proxy);
            }

            return childrenList.ToArray();
        }

        protected List<IModuleData> ClonedChildren()
        {
            if (Children == null || Children.Count == 0)
            {
                return new List<IModuleData>();
            }

            var children = new List<IModuleData>();
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child == null) continue;
                var clonedChild = child.Clone();
                if (clonedChild == null) continue;
                children.Add(clonedChild);
            }

            return children;
        }
    }
}