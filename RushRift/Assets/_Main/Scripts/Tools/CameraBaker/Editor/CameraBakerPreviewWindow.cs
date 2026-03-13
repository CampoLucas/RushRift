using UnityEditor;
using UnityEngine;

namespace Game.Tools.CameraBaker.Editor
{
    public class CameraBakerPreviewWindow : EditorWindow
    {
        private Texture2D _image;
        private string _assetPath;
        private Vector2 _scroll;

        public static void ShowPreview(string assetPath)
        {
            var window = GetWindow<CameraBakerPreviewWindow>("Baked Image");
            window.minSize = new Vector2(256, 256);
            window.SetImage(assetPath);
            window.Show();
        }

        private void SetImage(string assetPath)
        {
            _assetPath = assetPath;
            _image = AssetDatabase.LoadAssetAtPath<Texture2D>(_assetPath);
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                GUI.enabled = !string.IsNullOrEmpty(_assetPath);
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(_assetPath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                GUI.enabled = true;
            }

            if (_image == null)
            {
                EditorGUILayout.HelpBox("No baked image loaded.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_assetPath, EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var width = position.width - 24f;
            var aspect = (float)_image.width / _image.height;
            var height = width / Mathf.Max(0.0001f, aspect);

            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, _image, null, ScaleMode.ScaleToFit);

            EditorGUILayout.EndScrollView();
        }
    }
}