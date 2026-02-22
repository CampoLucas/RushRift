using UnityEditor;
using UnityEngine;

namespace Game.Tools.MeshCombiner.Editor
{
    public class PivotOptionsPopup : PopupWindowContent
    {
        private readonly MeshCombinerWindow _w;

        public PivotOptionsPopup(MeshCombinerWindow w) => _w = w;

        public override Vector2 GetWindowSize() => new Vector2(260, 70);

        public override void OnGUI(Rect rect)
        {
            _w.PivotGizmosOptions(rect);
            // using (new EditorGUILayout.VerticalScope())
            // {
            //     _w._showPivot = EditorGUILayout.ToggleLeft("Show pivot", _w._showPivot);
            //
            //     EditorGUI.BeginDisabledGroup(_w._wantsDisablePivotOptions()); // helper below
            //     _w._pivotSphereSize = EditorGUILayout.Slider("Marker size", _w._pivotSphereSize, 0.01f, 2f);
            //     EditorGUI.EndDisabledGroup();
            // }

            if (GUI.changed)
            {
                _w.Repaint();
                SceneView.RepaintAll();
            }
        }
    }
}