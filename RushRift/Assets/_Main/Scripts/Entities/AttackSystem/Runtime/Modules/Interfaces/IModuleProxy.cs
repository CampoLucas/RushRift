using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public interface IModuleProxy : System.IDisposable
    {
        float Duration { get; }
        float Timer { get; }
        int Count { get; }

        // Execution Methods
        void Init();
        bool Execute(ModuleParams mParams, float delta);
        void Reset();
        
        // Observer Getters
        bool TryGetStart(out IObserver<ModuleParams> observer);
        bool TryGetEnd(out IObserver<ModuleParams> observer);
        bool TryGetUpdate(out IObserver<ModuleParams, float> observer);
        
        // Composite Methods
        void AddChild(IModuleProxy child, bool initChildren = true);
        void AddChild(IModuleProxy[] children, bool initChildren = true);
        bool RemoveChild(IModuleProxy child);
        void ClearChildren(bool disposeChildren = true);
        
        // Getters for Children
        bool TryGetChild<TModule>(out TModule child, bool first = true) where TModule : IModuleProxy;
        bool TryGetChildren<TModule>(out TModule[] children) where TModule : IModuleProxy;
    }
}