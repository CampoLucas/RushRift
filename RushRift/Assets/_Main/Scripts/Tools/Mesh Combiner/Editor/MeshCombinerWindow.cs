using System.Collections.Generic;
using System.IO;
using Game.Utils;
using UnityEditor;
using UnityEngine;

namespace Game.Tools.MeshCombiner.Editor
{
    public enum McMaterial {
        First, PreserveAll, SkipDuplicates
    }
    
    public class MeshCombinerWindow : EditorWindow
    {
        private static GUIContent _gizmosIcon;
        private static GUIContent _focusIcon;
        private static GUIContent _clearMeshesIcon;
        private static GUIContent _localSpaceIcon;
        private static GUIContent _globalSpaceIcon;
        
        private const int LabelMaxWidth = 149;
        
        [SerializeField] private List<MeshFilter> meshFilters = new();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Transform _pivot;
        private bool _localPivotOffset = true;
        private Vector3 _pivotOffset;

        private McMaterial _material;
        private bool _replaceSelected = false;
        private string _folderPath = "Assets/_Generated/Combined Meshes";
        private string _fileName = "Combined Mesh";
        
        private CombineInstance[] _combine;
        
        private SerializedObject _serializedObject;
        private SerializedProperty _meshFiltersProp;

        #region Debug

        private bool _showPivot = true;
        private float _pivotSphereSize = 0.15f;

        #endregion

        [MenuItem("Tools/Generated/Mesh Combiner")]
        public static void Open()
        {
            GetWindow<MeshCombinerWindow>("Mesh Combiner");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _meshFiltersProp = _serializedObject.FindProperty("meshFilters");
            
            // Icon names are internal, so use fallbacks
            _gizmosIcon ??= Icon("d_GizmosToggle", "GizmosToggle", "Show pivot gizmo");
            _focusIcon ??= Icon("d_SceneViewCamera", "SceneViewCamera", "Focus Scene View on pivot");
            _clearMeshesIcon ??= Icon("d_TreeEditor.Trash", "TreeEditor.Trash", "Focus Scene View on pivot");
            _localSpaceIcon ??= Icon("d_ToolHandleLocal", "ToolHandleLocal", "Offset is in pivot local space");
            _globalSpaceIcon ??= Icon("d_ToolHandleGlobal", "ToolHandleGlobal", "Offset is in global space");
            
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            EditorGUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 35
            };
            EditorGUILayout.LabelField("Mesh Combiner v2", titleStyle, GUILayout.MinHeight(30));
            EditorGUILayout.Space(20);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Meshes Target", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("From Selection", EditorStyles.toolbarButton))
                {
                    GetMeshFilterFromSelection();
                }
                EditorGUI.BeginDisabledGroup(_meshFilter.IsNullOrMissing() || _meshRenderer.IsNullOrMissing());
                if (GUILayout.Button(_clearMeshesIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    _meshRenderer = null;
                    _meshFilter = null;
                }
                EditorGUI.EndDisabledGroup();
            }
            
            _meshFilter = (MeshFilter)EditorGUILayout.ObjectField("Mesh Filter", _meshFilter, typeof(MeshFilter), true);
            _meshRenderer = (MeshRenderer)EditorGUILayout.ObjectField("Mesh Renderer", _meshRenderer, typeof(MeshRenderer), true);

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Meshes To combine", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("To Selection", GUILayout.Width(70)))
                {
                    GetMeshFilterFromSelection();
                }
                if (GUILayout.Button("From Selection", EditorStyles.toolbarButton))
                {
                    GetMeshesFromSelection();
                }
                EditorGUI.BeginDisabledGroup(meshFilters == null || meshFilters.Count == 0);
                if (GUILayout.Button(_clearMeshesIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ClearMeshes();
                }
                EditorGUI.EndDisabledGroup();
            }
            
            EditorGUILayout.PropertyField(_meshFiltersProp, true);

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Pivot", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Selection", EditorStyles.toolbarButton))
                {
                    GetPivotFromSelection();
                }
                
                EditorGUI.BeginDisabledGroup(_pivot == null);
                
                // icon + text that changes depending on current mode
                var spaceContent = _localPivotOffset
                    ? new GUIContent(" Local", _localSpaceIcon.image, "Offset is in pivot local space")
                    : new GUIContent(" Global", _globalSpaceIcon.image, "Offset is in global space");

                // Draw as a pressed/unpressed toolbar button
                _localPivotOffset = GUILayout.Toggle(_localPivotOffset, spaceContent, EditorStyles.toolbarButton, GUILayout.Width(70));
                

                if (GUILayout.Button(_focusIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    FocusSceneViewOnPivot();
                }
                
                _showPivot = GUILayout.Toggle(_showPivot, _gizmosIcon, EditorStyles.toolbarButton, GUILayout.Width(28));

                // dropdown arrow button
                var dropIcon = EditorGUIUtility.IconContent("d_icon dropdown");
                if (dropIcon.image == null) dropIcon = EditorGUIUtility.IconContent("icon dropdown");

                if (GUILayout.Button(dropIcon, EditorStyles.toolbarDropDown, GUILayout.Width(18)))
                {
                    // Pick a reasonable popup size for clamping.
                    // If your popup is dynamic, choose the max you expect.
                    var popupSize = new Vector2(260, 140);
                    var anchor = new Rect(Event.current.mousePosition, popupSize);
                    
                    PopupWindow.Show(anchor, new PivotOptionsPopup(this));
                }
                EditorGUI.EndDisabledGroup();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("New Mesh Pivot", GUILayout.MaxWidth(LabelMaxWidth));
                _pivot = (Transform)EditorGUILayout.ObjectField(GUIContent.none, _pivot, typeof(Transform), true, GUILayout.MinWidth(30));
            }

            if (_pivot)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Offset", GUILayout.MaxWidth(LabelMaxWidth));
                    _pivotOffset = EditorGUILayout.Vector3Field(GUIContent.none, _pivotOffset);
                }

                //_pivotSphereSize = EditorGUILayout.Slider("Marker Size", _pivotSphereSize, 0.01f, 2f);
            }
            
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Material", EditorStyles.boldLabel);
            }

            _material = (McMaterial)EditorGUILayout.EnumPopup("Material", _material);

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Output", EditorStyles.boldLabel);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                _fileName = EditorGUILayout.TextField("File Name", _fileName);
            }
            
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Folder", GUILayout.MaxWidth(LabelMaxWidth));
                //GUILayout.FlexibleSpace();
                
                //EditorGUILayout.SelectableLabel(_folderPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(GUIContent.none, _folderPath,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("Pick", GUILayout.Width(70)))
                    PickFolderInsideAssets();
            }
            
            _serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(_meshFilter.IsNullOrMissing() || _meshRenderer.IsNullOrMissing() || meshFilters == null || meshFilters.Count == 0);
                if (GUILayout.Button("Combine"))
                {
                    CombineMeshes();
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.BeginDisabledGroup(_meshFilter.IsNullOrMissing() || _meshRenderer.IsNullOrMissing());
                if (GUILayout.Button("Save"))
                {
                    SaveMesh();
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPivot || !_pivot) return;

            var p = GetPivotWorldPosition();

            var oldZ = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            // sphere marker
            Handles.SphereHandleCap(0, p, Quaternion.identity, HandleUtility.GetHandleSize(p) * _pivotSphereSize, EventType.Repaint);
            Handles.Label(p, "Pivot");
            Handles.zTest = oldZ;
        }

        public void PivotGizmosOptions(Rect position)
        {
            _pivotSphereSize = EditorGUILayout.Slider("Marker Size", _pivotSphereSize, 0.01f, 2f);
        }
        
        private static GUIContent Icon(string name, string fallback, string tooltip = null)
        {
            var c = EditorGUIUtility.IconContent(name);
            if (c == null || c.image == null)
                c = EditorGUIUtility.IconContent(fallback);

            if (!string.IsNullOrEmpty(tooltip))
                c.tooltip = tooltip;

            return c;
        }
        
        private void ClearMeshes()
        {
            meshFilters.Clear();
        }

        private void GetMeshesFromSelection()
        {
            ClearMeshes();
            
            // foreach (var go in Selection.gameObjects)
            // {
            //     var mf = go.GetComponent<MeshFilter>();
            //     if (mf != null)
            //         meshFilters.Add(mf);
            // }
            
            foreach (var go in Selection.gameObjects)
            {
                var filters = go.GetComponentsInChildren<MeshFilter>(true);
                meshFilters.AddRange(filters);
            }
        }

        private void GetMeshFilterFromSelection()
        {
            _meshFilter = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<MeshFilter>() : null;
            _meshRenderer = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<MeshRenderer>() : null;
        }

        private void GetPivotFromSelection()
        {
            var p = Selection.activeGameObject ? Selection.activeGameObject.transform : null;
            if (p) _pivot = p;
        }
        
        private Vector3 GetPivotWorldPosition()
        {
            if (!_pivot) return Vector3.zero;

            // Offset in local space
            if (_localPivotOffset)
            {
                return _pivot.TransformPoint(_pivotOffset);
            }

            // Offset in world space
            return _pivot.position + _pivotOffset;
        }
        
        private void FocusSceneViewOnPivot()
        {
            var sv = SceneView.lastActiveSceneView;
            if (!sv) return;

            var p = GetPivotWorldPosition();

            // keep current rotation and size, just move to pivot
            sv.LookAt(p, sv.rotation, sv.size);
            sv.Repaint();
        }
        
        private Rect GetScreenRectFromGUIRect(Rect guiRect)
        {
            // IMGUI rect -> UIElements panel coords
            var world = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));

            // The above can still be wrong in docked layouts on some versions.
            // This one is the most reliable when available:
            if (rootVisualElement != null && rootVisualElement.panel != null)
            {
                // guiRect.position is IMGUI local. Convert to panel/world, then to screen.
                Vector2 panelPos = guiRect.position;
                // IMGUI is inside the window. Offset by window position in screen space.
                panelPos += position.position;

                return new Rect(panelPos.x, panelPos.y, guiRect.width, guiRect.height);
            }

            return new Rect(world.x, world.y, guiRect.width, guiRect.height);
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
        
        public void CombineMeshes()
        {
            _combine = new CombineInstance[meshFilters.Count];

            var i = 0;
            while (i < meshFilters.Count)
            {
                _combine[i].mesh = meshFilters[i].sharedMesh;
                _combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);

                i++;
            }

            _meshFilter ??= meshFilters[0];
            _meshRenderer ??= meshFilters[0].GetComponent<MeshRenderer>();
            
            var mesh = new Mesh();
            _meshFilter.mesh = mesh;
            _meshFilter.sharedMesh = mesh;
            _meshFilter.sharedMesh.CombineMeshes(_combine);

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            if (meshFilters.Count > 0)
            {
                _meshRenderer.sharedMaterial = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
            }

        }
        
        public void SaveMesh()
        {
#if UNITY_EDITOR
            var filePath = _folderPath + "/" + _fileName + ".asset";
            AssetDatabase.CreateAsset(_meshFilter.sharedMesh, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Combined mesh saved at: " + filePath);
#endif
        }
    }
}
