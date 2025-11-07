using UnityEngine;

namespace Game.UI
{
    public static class CursorHandler
    {
        public static CursorLockMode lockState
        {
            get => Cursor.lockState;
            set => Cursor.lockState = value;
        }
        
        public static bool visible
        {
            get => Cursor.visible;
            set => Cursor.visible = value;
        }
        
        
    }
}