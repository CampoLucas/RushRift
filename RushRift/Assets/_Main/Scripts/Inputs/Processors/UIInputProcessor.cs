using System;
using Game.DesignPatterns.Observers;
using MyTools.Global;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Game.InputSystem.UI
{
    public abstract class UIInputProcessor : MonoBehaviour
    {
        private NullCheck<ProcessableUIInputModule> _module;
        private NullCheck<ActionObserver<InputSystemUIInputModule>> _preProcess;
        private NullCheck<ActionObserver<InputSystemUIInputModule>> _postProcess;

        private void OnEnable()
        {
            if (!_module && !_module.Set(GetComponent<ProcessableUIInputModule>(), FindObjectOfType<ProcessableUIInputModule>))
            {
                this.Log("Couldn't find a UI Module on the scene. The component will disable itself.", LogType.Error);
                enabled = false;
            }

            if (!_preProcess)
            {
                _preProcess.Set(new ActionObserver<InputSystemUIInputModule>(OnPreProcess));
            }

            if (!_postProcess)
            {
                _postProcess.Set(new ActionObserver<InputSystemUIInputModule>(OnPostProcess));
            }
            
            if (_module.TryGet(out var module) 
                && _preProcess.TryGet(out var pre, () => new ActionObserver<InputSystemUIInputModule>(OnPreProcess)) 
                && _postProcess.TryGet(out var post, () => new ActionObserver<InputSystemUIInputModule>(OnPostProcess)))
            {
                module.AttachProcess(_preProcess.Get(), _postProcess.Get());
            }
        }

        private void OnDisable()
        {
            if (_module.TryGet(out var module) && _preProcess.TryGet(out var pre) && _postProcess.TryGet(out var post))
            {
                module.DetachProcess(pre, post);
            }
        }

        protected abstract void OnPreProcess(InputSystemUIInputModule module);
        protected abstract void OnPostProcess(InputSystemUIInputModule module);

        protected virtual void OnDestroy()
        {
            _preProcess.Dispose();
            _postProcess.Dispose();
            _module.Dispose();
        }
    }
}