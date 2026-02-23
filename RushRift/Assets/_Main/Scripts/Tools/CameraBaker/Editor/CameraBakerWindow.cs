using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tools.CameraBaker.Editor
{
    public class CameraBakerWindow : EditorWindow
    {
        private Camera _camera;

        private int _width = 512;
        private int _height = 512;

        private bool _lockAspect = true;
        private Vector2 _aspectRatio = Vector2.one;
        private float _aspect;
        
        private string _folderPath = "Assets/_LevelPreview";
        private string _fileName = "Preview.png";

        private bool _importAsSprite = true;
        private bool _mipmaps = false;
        private bool _srgb = true;
        private bool _alphaIsTransparency = true;
        
        [MenuItem("Tools/Level Previews/Baker Window")]
        public static void Open()
        {
            GetWindow<CameraBakerWindow>("Camera Baker");
        }

        private void OnEnable()
        {
            // Default camera from selection if possible
            var cam = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Camera>() : null;
            if (cam) _camera = cam;

            SyncAspectFromSize();
            SyncDefaultName();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _camera = (Camera)EditorGUILayout.ObjectField("Camera", _camera, typeof(Camera), true);

                if (GUILayout.Button("Use Selected", GUILayout.Width(110)))
                {
                    var cam = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Camera>() : null;
                    if (cam) _camera = cam;
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            
            _lockAspect = EditorGUILayout.Toggle("Lock Aspect", _lockAspect);

            if (_lockAspect)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _aspect = EditorGUILayout.FloatField("Aspect (W/H)", _aspect);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _aspect = Mathf.Max(0.0001f, _aspect);
                        if (_lockAspect)
                            _height = Mathf.Max(1, Mathf.RoundToInt(_width / _aspect));
                    }

                    if (GUILayout.Button("From Size", GUILayout.Width(90)))
                        SyncAspectFromSize();

                    if (GUILayout.Button("1:1", GUILayout.Width(40)))
                        SetPreset(512, 512);

                    if (GUILayout.Button("16:9", GUILayout.Width(50)))
                        SetPreset(1920, 1080);

                    if (GUILayout.Button("16:10", GUILayout.Width(50)))
                        SetPreset(1920, 1200);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _width = Mathf.Max(1, EditorGUILayout.IntField("Width", _width));
                if (EditorGUI.EndChangeCheck() && _lockAspect)
                {
                    _height = Mathf.Max(1, Mathf.RoundToInt(_width / Mathf.Max(0.0001f, _aspect)));
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _height = Mathf.Max(1, EditorGUILayout.IntField("Height", _height));
                if (EditorGUI.EndChangeCheck() && _lockAspect)
                {
                    _width = Mathf.Max(1, Mathf.RoundToInt(_height * _aspect));
                }
            }
            
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            
            using (new EditorGUILayout.HorizontalScope())
            {
                _fileName = EditorGUILayout.TextField("File Name", _fileName);
                if (GUILayout.Button("Auto", GUILayout.Width(70)))
                {
                    SyncDefaultName();
                }
            }
            
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Folder", GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(_folderPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                
                if (GUILayout.Button("Pick...", GUILayout.Width(70)))
                    PickFolderInsideAssets();
            }
            
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
            _importAsSprite = EditorGUILayout.Toggle("Import as Sprite", _importAsSprite);
            using (new EditorGUI.DisabledScope(!_importAsSprite))
            {
                _mipmaps = EditorGUILayout.Toggle("Mip Maps", _mipmaps);
                _srgb = EditorGUILayout.Toggle("sRGB", _srgb);
                _alphaIsTransparency = EditorGUILayout.Toggle("Alpha is Transparency", _alphaIsTransparency);
            }
            
            EditorGUILayout.Space(10);
            
            using (new EditorGUI.DisabledScope(!CanBake()))
            {
                if (GUILayout.Button("Bake"))
                {
                    EnsurePngExtension();
                    CameraBakerTool.Bake(
                        _camera,
                        _width,
                        _height,
                        _folderPath,
                        _fileName,
                        _importAsSprite,
                        _mipmaps,
                        _srgb,
                        _alphaIsTransparency
                    );
                }
            }

            EditorGUILayout.LabelField("Quick Instructions");
            EditorGUILayout.HelpBox("Use the prefab 'P_Baking Camera'.", MessageType.Info);
            EditorGUILayout.HelpBox("When the camera is selected, press CTRL + SHIFT + F to change the position and rotation to the scene's view.", MessageType.Info);
            
            EditorGUILayout.LabelField("Instrucciones rápidas");
            EditorGUILayout.HelpBox("Usar la cámara prefabricada 'P_Baking Camera'.", MessageType.Info);
            EditorGUILayout.HelpBox("Cuando la cámara esté seleccionada, presione CTRL + MAYÚS + F para cambiar la posición y la rotación a la vista de la escena.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (!CanBake())
            {
                EditorGUILayout.HelpBox("Assign a Camera and choose a folder inside Assets.", MessageType.Warning);
            }
            
        }

        private bool CanBake()
        {
            if (!_camera) return false;
            if (string.IsNullOrWhiteSpace(_folderPath)) return false;
            return _folderPath.StartsWith("Assets");
        }
        
        private void PickFolderInsideAssets()
        {
            var abs = EditorUtility.OpenFolderPanel("Pick output folder (inside Assets)", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            abs = abs.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            if (!abs.StartsWith(dataPath))
            {
                Debug.LogError("Folder must be inside this project's Assets folder.");
                return;
            }

            _folderPath = "Assets" + abs.Substring(dataPath.Length);
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
        }

        private void SyncAspectFromSize()
        {
            _aspect = _width / Mathf.Max(1f, _height);
            _aspect = Mathf.Max(0.0001f, _aspect);
        }
        
        private void SetPreset(int w, int h)
        {
            _width = w;
            _height = h;
            SyncAspectFromSize();
            SyncDefaultName();
        }
        
        private void SyncDefaultName()
        {
            var sceneName = _camera && _camera.gameObject.scene.IsValid()
                ? _camera.gameObject.scene.name
                : "Preview";

            _fileName = $"{sceneName}({_width}x{_height}).png";
        }
        
        private void EnsurePngExtension()
        {
            if (string.IsNullOrWhiteSpace(_fileName))
                _fileName = "Preview.png";

            if (!Path.HasExtension(_fileName))
                _fileName += ".png";
            else if (Path.GetExtension(_fileName).ToLowerInvariant() != ".png")
                _fileName = Path.ChangeExtension(_fileName, ".png");
        }
    }
}