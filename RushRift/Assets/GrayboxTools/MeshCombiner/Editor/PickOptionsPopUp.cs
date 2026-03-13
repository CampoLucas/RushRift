using System;
using UnityEditor;
using UnityEngine;

namespace GrayboxTools.MeshCombiner.Editor
{
    public class OptionsPopup : PopupWindowContent
    {
        private readonly EditorWindow _window;
        private readonly Action<Rect> _onOpened;
        private readonly Vector2 _windowSize;

        public OptionsPopup(EditorWindow window, Action<Rect> onOpened, Vector2 windowSize)
        {
            _window = window;
            _onOpened = onOpened;
            _windowSize = windowSize;
        }

        public override Vector2 GetWindowSize() => _windowSize;

        public override void OnGUI(Rect rect)
        {
            _onOpened?.Invoke(rect);
            
            if (GUI.changed)
            {
                _window.Repaint();
                SceneView.RepaintAll();
            }
        }
    }
}