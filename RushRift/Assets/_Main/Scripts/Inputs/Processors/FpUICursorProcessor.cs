using System;
using MyTools.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.InputSystem.UI
{
    public class FpUICursorProcessor : UIInputProcessor
    {
        private CursorLockMode _lockState;
        
        protected override void OnPreProcess(InputSystemUIInputModule module)
        {
            _lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
        }

        protected override void OnPostProcess(InputSystemUIInputModule module)
        {
            Cursor.lockState = _lockState;
        }
    }
}