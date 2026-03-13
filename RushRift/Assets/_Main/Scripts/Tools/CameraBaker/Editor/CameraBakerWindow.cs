using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tools.CameraBaker.Editor
{
    public class CameraBakerWindow : EditorWindow
    {
        private const float CollapseWidth = 480f;
        private const string FolderKey = "CameraBakerWindow.FolderPath";
        private const string FileKey = "CameraBakerWindow.FileName";
        private const string LastBakeKey = "CameraBakerWindow.LastBakePath";
        private const string AspectKey = "CameraBakerWindow.Aspect";
        private const string LockAspectKey = "CameraBakerWindow.LockAspect";
        private const string WidthKey = "CameraBakerWindow.Width";
        private const string HeightKey = "CameraBakerWindow.Height";
        
        private Camera _camera;

        private int _width = 512;
        private int _height = 512;

        private bool _lockAspect = true;
        private float _aspect;
        
        private string _folderPath = "Assets/_Generated/Level Preview";
        private string _fileName = "Preview.png";

        private bool _importAsSprite = true;
        private bool _mipmaps;
        private bool _srgb = true;
        private bool _alphaIsTransparency = true;
        
        [MenuItem("Tools/Graybox Tools/Baker Window")]
        public static void Open()
        {
            GetWindow<CameraBakerWindow>("Camera Baker");
        }

        private void OnEnable()
        {
            _folderPath = EditorPrefs.GetString(FolderKey, _folderPath);
            _fileName = EditorPrefs.GetString(FileKey, _fileName);
            
            _width = EditorPrefs.GetInt(WidthKey, _width);
            _height = EditorPrefs.GetInt(HeightKey, _height);
            
            _aspect = EditorPrefs.GetFloat(AspectKey, (float)_width / Mathf.Max(1, _height));
            _lockAspect = EditorPrefs.GetBool(LockAspectKey, true);
            
            // Default camera from selection if possible
            var cam = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Camera>() : null;
            if (cam)
            {
                _camera = cam;
            }

            // if (_aspect <= 0f)
            // {
            //     SyncAspectFromSize();
            // }

            if (string.IsNullOrWhiteSpace(_fileName))
            {
                SyncDefaultName();
            }
        }
        
        private void OnDisable()
        {
            SavePrefs();
        }

        private void OnDestroy()
        {
            SavePrefs();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                _camera = (Camera)EditorGUILayout.ObjectField("Camera", _camera, typeof(Camera), true);
                
                DrawActionRow("Select", 65f,
                    new RowAction("Use Selected", 110f, () =>
                    {
                        var cam = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Camera>() : null;
                        if (cam) _camera = cam;
                    }),
                    new RowAction("Main", 45f, UseMainCamera));
                
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
                        {
                            _height = Mathf.Max(1, Mathf.RoundToInt(_width / _aspect));
                        }
                    }
                    
                    DrawActionRow("Aspect", 65f,
                        new RowAction("From Size", 90f, SyncAspectFromSize),
                        new RowAction("1:1", 40f, () => SetPreset(512, 512)),
                        new RowAction("16:9", 50f, () => SetPreset(1920, 1080)),
                        new RowAction("16:10", 50f, () => SetPreset(1920, 1200)));

                    // if (GUILayout.Button("From Size", GUILayout.Width(90)))
                    //     SyncAspectFromSize();
                    //
                    // if (GUILayout.Button("1:1", GUILayout.Width(40)))
                    //     SetPreset(512, 512);
                    //
                    // if (GUILayout.Button("16:9", GUILayout.Width(50)))
                    //     SetPreset(1920, 1080);
                    //
                    // if (GUILayout.Button("16:10", GUILayout.Width(50)))
                    //     SetPreset(1920, 1200);
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
                    BakeNow();
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = HasLastBake();
                if (GUILayout.Button("Select Baked Image"))
                {
                    SelectLastBake();
                }

                if (GUILayout.Button("Show Baked Image"))
                {
                    ShowLastBake();
                }
                GUI.enabled = true;
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
            
            SavePrefs();
        }
        
        private void BakeNow()
        {
            EnsurePngExtension();

            var fullPath = Path.Combine(_folderPath, _fileName).Replace('\\', '/');
            if (File.Exists(fullPath))
            {
                var replace = EditorUtility.DisplayDialog(
                    "Replace Image",
                    $"The image already exists:\n{fullPath}\n\nDo you want to replace it?",
                    "Replace",
                    "Cancel");

                if (!replace)
                    return;
            }

            var bakedPath = CameraBakerTool.Bake(
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

            if (string.IsNullOrEmpty(bakedPath))
                return;

            EditorPrefs.SetString(LastBakeKey, bakedPath);

            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(bakedPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            CameraBakerPreviewWindow.ShowPreview(bakedPath);
        }

        private bool CanBake()
        {
            if (!_camera) return false;
            if (string.IsNullOrWhiteSpace(_folderPath)) return false;
            return _folderPath.StartsWith("Assets");
        }

        private bool UseDropdown()
        {
            return position.width < CollapseWidth;
        }
        
        private void DrawInlineOrDropdown(
    string label,
    System.Action drawMain,
    string dropdownName,
    params RowAction[] actions)
{
    if (UseDropdown())
    {
        drawMain?.Invoke();

        if (GUILayout.Button(dropdownName, EditorStyles.popup))
        {
            var menu = new GenericMenu();

            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];

                if (action.Disabled)
                    menu.AddDisabledItem(new GUIContent(action.Name));
                else
                    menu.AddItem(new GUIContent(action.Name), false, action.Action);
            }

            menu.ShowAsContext();
        }

        return;
    }

    using (new EditorGUILayout.HorizontalScope())
    {
        drawMain?.Invoke();

        for (int i = 0; i < actions.Length; i++)
        {
            var action = actions[i];

            using (new EditorGUI.DisabledScope(action.Disabled))
            {
                if (GUILayout.Button(action.Name, GUILayout.Width(action.Width)))
                    action.Action?.Invoke();
            }
        }
    }
}
        
        private void DrawActionRow(string label, float labelWidth, params RowAction[] actions)
        {
            if (UseDropdown())
            {
                if (GUILayout.Button(label, EditorStyles.popup, GUILayout.MaxWidth(labelWidth)))
                {
                    var menu = new GenericMenu();

                    for (int i = 0; i < actions.Length; i++)
                    {
                        var action = actions[i];
                        if (action.Disabled)
                            menu.AddDisabledItem(new GUIContent(action.Name));
                        else
                            menu.AddItem(new GUIContent(action.Name), false, action.Action);
                    }

                    menu.ShowAsContext();
                }

                return;
            }

            var width = 0f;

            for (var i = 0; i < actions.Length; i++)
            {
                width += actions[i].Width;
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(width)))
            {
                for (var i = 0; i < actions.Length; i++)
                {
                    var action = actions[i];

                    using (new EditorGUI.DisabledScope(action.Disabled))
                    {
                        if (GUILayout.Button(action.Name, GUILayout.Width(action.Width)))
                            action.Action?.Invoke();
                    }
                }
            }
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
        
        private void SavePrefs()
        {
            EditorPrefs.SetString(FolderKey, _folderPath);
            EditorPrefs.SetString(FileKey, _fileName);
            EditorPrefs.SetFloat(AspectKey, _aspect);
            EditorPrefs.SetBool(LockAspectKey, _lockAspect);
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
        
        private bool HasLastBake()
        {
            var path = EditorPrefs.GetString(LastBakeKey, string.Empty);
            return !string.IsNullOrWhiteSpace(path);
        }

        private void SelectLastBake()
        {
            var path = EditorPrefs.GetString(LastBakeKey, string.Empty);
            if (string.IsNullOrWhiteSpace(path))
                return;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void ShowLastBake()
        {
            var path = EditorPrefs.GetString(LastBakeKey, string.Empty);
            if (string.IsNullOrWhiteSpace(path))
                return;

            CameraBakerPreviewWindow.ShowPreview(path);
        }

        private void UseMainCamera()
        {
            var cam = Camera.main;

            if (!cam)
            {
                var allCams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allCams.Length; i++)
                {
                    if (allCams[i].CompareTag("MainCamera"))
                    {
                        cam = allCams[i];
                        break;
                    }
                }
            }

            if (!cam)
            {
                Debug.LogWarning("No camera with the 'MainCamera' tag was found.");
                return;
            }

            _camera = cam;
            SyncDefaultName();
            Repaint();
        }
    }
}