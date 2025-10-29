using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.InputSystem.UI
{
    [AddComponentMenu("Event/Processable UI Input Module (New Input System)")]
    public class ProcessableUIInputModule : InputSystemUIInputModule
    {
        private NullCheck<Subject<InputSystemUIInputModule>> _preProcessors;
        private NullCheck<Subject<InputSystemUIInputModule>> _postProcessors;

        public override void Process()
        {
            if (_preProcessors.TryGet(out var process))
            {
                process.NotifyAll(this);
            }
            
            base.Process();

            if (_postProcessors.TryGet(out process))
            {
                process.NotifyAll(this);
            }
        }

        public void AttachProcess(IObserver<InputSystemUIInputModule> pre, IObserver<InputSystemUIInputModule> post)
        {
            // If it doesn't have a processor subject, it creates it.
            if (_preProcessors.TryGet(out var process, CreateIfNull))
            {
                process.Attach(pre);
            }

            if (_postProcessors.TryGet(out process, CreateIfNull))
            {
                process.Attach(post);
            }
        }
        
        public void DetachProcess(IObserver<InputSystemUIInputModule> pre, IObserver<InputSystemUIInputModule> post)
        {
            // If it only detaches the processor if it has a subject
            if (_preProcessors.TryGet(out var process))
            {
                process.Detach(pre);
            }
            
            if (_postProcessors.TryGet(out process))
            {
                process.Detach(post);
            }
        }

        private Subject<InputSystemUIInputModule> CreateIfNull() => new Subject<InputSystemUIInputModule>();

        protected override void OnDestroy()
        {
            // The dispose detaches all observers and gets rid of the reference.
            _preProcessors.Dispose();
            _postProcessors.Dispose();
            
            base.OnDestroy();
        }
        
#if UNITY_EDITOR
        public void SetActionsAsset(InputActionAsset asset, bool reassignDefaults = true)
        {
            actionsAsset = asset;
            if (reassignDefaults)
                AssignDefaultActions();
        }
#endif
    }
}