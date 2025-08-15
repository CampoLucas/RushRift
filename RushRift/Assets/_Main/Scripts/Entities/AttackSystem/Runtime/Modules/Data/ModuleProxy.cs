using System;
using Game.DesignPatterns.Observers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.AttackSystem
{
    public abstract class ModuleProxy<TData> : IModuleProxy
        where TData : IModuleData
    {
        protected TData Data { get; private set; }
        public float Timer { get; private set; }
        public float Duration => Data.Duration;
        public int Count => _children.Count;
        
        // Stores it's start, update and end methods in observers,
        // so in the case it is used as a child of another module, it's methods can be called more efficiently 
        protected DesignPatterns.Observers.IObserver<ModuleParams> StartObserver;
        protected DesignPatterns.Observers.IObserver<ModuleParams> EndObserver;
        protected IObserver<ModuleParams, float> UpdateObserver;
        protected IObserver<ModuleParams, float> LateUpdateObserver;
        
        private ModuleExecution ModuleExecution => Data.GetExecution();
        
        // children modules attach to the parent module subject,
        // that way only the necessary start, update and end are called.
        private ISubject<ModuleParams> _onStartSubject = new Subject<ModuleParams>();
        private ISubject<ModuleParams> _onEndSubject = new Subject<ModuleParams>();
        private ISubject<ModuleParams, float> _onUpdateSubject = new Subject<ModuleParams, float>();

        private IModuleProxy[] _childrenToAdd;
        private List<IModuleProxy> _children;
        private Dictionary<Type, List<IModuleProxy>> _childModules;
        private bool _started;
        private bool _running;
        private int _childIndex;
        protected readonly bool DisposeData;

        
        protected ModuleProxy(TData data, IModuleProxy[] children, bool disposeData = false)
        {
            DisposeData = disposeData;
            Data = data;
            
            _childrenToAdd = children;
            _children = new List<IModuleProxy>();
            _childModules = new Dictionary<Type, List<IModuleProxy>>();
            Init();
        }

        #region Public Methods

        public void Init()
        {
            BeforeInit();
            //if (TryGetStart(out var start)) _onStartSubject.Attach(start);
            //if (TryGetEnd(out var end)) _onEndSubject.Attach(end);
            //if (TryGetUpdate(out var update)) _onUpdateSubject.Attach(update);
            
            
            if (_childrenToAdd is { Length: > 0 })
            {
                AddChild(_childrenToAdd);
            }

            _childrenToAdd = null;
            AfterInit();
        }
        
        public bool Execute(ModuleParams mParams, float delta)
        {
            if (!_started && !_running)
            {
                _started = true;
                _running = true;
                
                Debug.Log("SuperTest: Notify Module Start");
                _onStartSubject.NotifyAll(mParams);
                for (var i = 0; i < _children.Count; i++)
                {
                    _children[i].Reset();
                }
            }

            _onUpdateSubject.NotifyAll(mParams, delta);
            
            if (FinishedExecuting(mParams, delta))
            {
                _onEndSubject.NotifyAll(mParams);
                _running = false;
                return false;
            }

            return true;
        }

        public void Reset()
        {
            _started = false;
            _running = false;
            Timer = 0;
            _childIndex = 0;

            for (var i = 0; i < _children.Count(); i++)
            {
                _children[i].Reset();
            }
        }
        
        /// <summary>
        /// Disposes all the references.
        /// </summary>
        public void Dispose()
        {
            OnDispose();

            // Dispose of TData
            if (DisposeData) Data.Dispose();
            Data = default;
            
            // Dispose all the subjects
            _onStartSubject.Dispose();
            _onStartSubject = null;
            _onEndSubject.Dispose();
            _onEndSubject = null;
            _onUpdateSubject.Dispose();
            _onUpdateSubject = null;
            
            // Dispose all observers
            if (StartObserver != null) StartObserver.Dispose();
            if (EndObserver != null) EndObserver.Dispose();
            if (UpdateObserver != null) UpdateObserver.Dispose();
            
            // Dispose all the children
            for (var i = 0; i < _children.Count; i++)
            {
                var c = _children[i];
                if (c == null) continue;
                
                c.Dispose();
            }
            _children.Clear();
        }
        
        public virtual void OnDraw(Transform origin) { }
        public virtual void OnDrawSelected(Transform origin) { }

        #region Observer Getters

        public virtual bool TryGetStart(out DesignPatterns.Observers.IObserver<ModuleParams> start)
        {
            if (StartObserver != null)
            {
                start = StartObserver;
                return true;
            }

            start = null;
            return false;
        }
        
        public virtual bool TryGetEnd(out DesignPatterns.Observers.IObserver<ModuleParams> end)
        {
            if (EndObserver != null)
            {
                end = EndObserver;
                return true;
            }

            end = null;
            return false;
        }
        
        public virtual bool TryGetUpdate(out IObserver<ModuleParams, float> update)
        {
            if (UpdateObserver != null)
            {
                update = UpdateObserver;
                return true;
            }

            update = null;
            return false;
        }
        
        public virtual bool TryGetLateUpdate(out IObserver<ModuleParams, float> lateUpdate)
        {
            if (LateUpdateObserver != null)
            {
                lateUpdate = LateUpdateObserver;
                return true;
            }

            lateUpdate = null;
            return false;
        }
        
        

        #endregion
        
        #region Composite Methods

        public void AddChild(IModuleProxy child, bool initChildren = true)
        {
            _children.Add(child);
            
            if (initChildren) child.Init();

            if (ModuleExecution == ModuleExecution.Parallel)
            {
                if (child.TryGetStart(out var start)) _onStartSubject.Attach(start);
                if (child.TryGetUpdate(out var update)) _onUpdateSubject.Attach(update);
                if (child.TryGetEnd(out var end)) _onEndSubject.Attach(end);
            }

            var type = child.GetType();

            while (type != null && typeof(IModuleProxy).IsAssignableFrom(type))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ModuleProxy<>))
                {
                    break;
                }
                
                if (!_childModules.TryGetValue(type, out var list))
                {
                    list = new List<IModuleProxy>();
                    _childModules[type] = list;
                }
                
                list.Add(child);

                type = type.BaseType;
            }
        }

        public void AddChild(IModuleProxy[] children, bool initChildren = true)
        {
            for (var i = 0; i < children.Length; i++)
            {
                AddChild(children[i], initChildren);
            }
        }

        public bool RemoveChild(IModuleProxy child)
        {
            if (!_children.Remove(child)) return false;

            if (ModuleExecution == ModuleExecution.Parallel)
            {
                if (child.TryGetStart(out var start)) _onStartSubject.Detach(start);
                if (child.TryGetUpdate(out var update)) _onUpdateSubject.Detach(update);
                if (child.TryGetEnd(out var end)) _onEndSubject.Detach(end);
            }
            
            var type = GetType();
            
            while (type != null && typeof(IModuleProxy).IsAssignableFrom(type))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ModuleProxy<>))
                {
                    break;
                }
                
                if (_childModules.TryGetValue(type, out var list))
                {
                    list.Remove(child);

                    if (list.Count == 0)
                    {
                        _childModules.Remove(type);
                    }
                }

                type = type.BaseType;
            }
            
            return true;
        }

        public void ClearChildren(bool disposeChildren = true)
        {
            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                
                if (child == null) continue;
                
                // if parallel, remove the children from subjects
                if (ModuleExecution == ModuleExecution.Parallel)
                {
                    if (child.TryGetStart(out var start)) _onStartSubject.Detach(start);
                    if (child.TryGetUpdate(out var update)) _onUpdateSubject.Detach(update);
                    if (child.TryGetEnd(out var end)) _onEndSubject.Detach(end);
                }
                
                // dispose all children in _children.
                
                if (disposeChildren) child.Dispose();
            }
            _children.Clear();
            _childModules.Clear();
        }

        public bool TryGetChild<TModule>(out TModule child, bool first = true) where TModule : IModuleProxy
        {
            child = default;

            if (!_childModules.TryGetValue(typeof(TModule), out var list) || list.Count == 0) return false;
            child = (TModule)(first ? list[0] : list[list.Count - 1]);
            return true;
        }

        public bool TryGetChildren<TModule>(out TModule[] children) where TModule : IModuleProxy
        {
            children = null;
            
            if (!_childModules.TryGetValue(typeof(TModule), out var list) || list.Count == 0) return false;
            children = list.Cast<TModule>().ToArray();
            return true;
        }

        #endregion

        #endregion

        #region Protected Methods

        // Execution Methods
        protected virtual void BeforeInit() { }
        protected virtual void AfterInit() { }
        protected virtual void OnDispose() { }

        protected virtual bool FinishedExecuting(ModuleParams mParams, float delta)
        {
            switch (ModuleExecution)
            {
                case ModuleExecution.Parallel:
                    Timer += delta;
                    return Timer >= Duration;
                case ModuleExecution.Sequential:
                    return ExecuteChildren(ref _children, ref _childIndex, mParams, delta);
                case ModuleExecution.Blend:
                    return true;
                default:
                    return true;
            }
            
        }

        protected bool ExecuteChildren(ref List<IModuleProxy> children, ref int index, ModuleParams mParams, float delta)
        {
            if (children == null || children.Count == 0 || index >= children.Count) return true;
            if (children[index].Execute(mParams, delta)) return false;

            index++;
            return index >= children.Count;
        }
        
        // Child Methods

        #endregion
    }

    public struct ModuleParams
    {
        public Transform OriginTransform;
        public Joints<EntityJoint> Joints;
        public NullCheck<IController> Owner;
    }

    public enum ModuleExecution
    {
        Parallel,
        Sequential,
        Blend,
    }
}