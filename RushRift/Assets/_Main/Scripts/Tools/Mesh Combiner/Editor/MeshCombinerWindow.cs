using System;
using System.Collections.Generic;
using System.IO;
using Game.Utils;
using MyTools.Global;
using UnityEditor;
using UnityEngine;

namespace Game.Tools.MeshCombiner.Editor
{
    public class MeshCombinerWindow : EditorWindow
    {
        private static GUIContent _gizmosIcon;
        private static GUIContent _focusIcon;
        private static GUIContent _localSpaceIcon;
        private static GUIContent _globalSpaceIcon;
        private static GUIContent _clearIcon;
        
        private const int LabelMaxWidth = 149;
        
        [SerializeField] private List<MeshFilter> meshFilters = new();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        
        // Pivot
        private McPivotApply _pivotApply = McPivotApply.MoveToPivot;
        private bool _localPivotOffset = true;
        private Transform _pivot;
        private Vector3 _pivotOffset;
        private Vector3 _pivotRotationOffset;

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
            _clearIcon ??= Icon("d_TreeEditor.Trash", "TreeEditor.Trash", "Clear");
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
                EditorGUI.BeginDisabledGroup(_meshFilter == null && _meshRenderer == null);
                if (GUILayout.Button(_clearIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
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
                GUILayout.Label("Meshes To Combine", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("From Selection", EditorStyles.toolbarButton))
                {
                    GetMeshesFromSelection();
                }
                EditorGUI.BeginDisabledGroup(meshFilters == null || meshFilters.Count == 0);
                if (GUILayout.Button(_clearIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
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
                var spaceContent = _pivot && _localPivotOffset
                    ? new GUIContent(" Local", _localSpaceIcon.image, "Offset is in pivot local space")
                    : new GUIContent(" Global", _globalSpaceIcon.image, "Offset is in global space");

                // Draw as a pressed/unpressed toolbar button
                if (!_pivot)
                {
                    GUILayout.Toggle(false, spaceContent, EditorStyles.toolbarButton, GUILayout.Width(70));
                }
                else
                {
                    _localPivotOffset = GUILayout.Toggle(_localPivotOffset, spaceContent, EditorStyles.toolbarButton, GUILayout.Width(70));
                }
                EditorGUI.EndDisabledGroup();
                

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
                
                EditorGUI.BeginDisabledGroup(IsPivotOptionsDefault());
                if (GUILayout.Button(_clearIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ClearPivotOptions();
                }
                EditorGUI.EndDisabledGroup();
            }
            
            _pivotApply = (McPivotApply)EditorGUILayout.EnumPopup("Apply Pivot", _pivotApply);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("New Mesh Pivot", GUILayout.MaxWidth(LabelMaxWidth));
                _pivot = (Transform)EditorGUILayout.ObjectField(GUIContent.none, _pivot, typeof(Transform), true, GUILayout.MinWidth(30));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Offset", GUILayout.MaxWidth(LabelMaxWidth));
                _pivotOffset = EditorGUILayout.Vector3Field(GUIContent.none, _pivotOffset);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Rotation", GUILayout.MaxWidth(LabelMaxWidth));
                _pivotRotationOffset = EditorGUILayout.Vector3Field(GUIContent.none, _pivotRotationOffset);
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

        private bool IsPivotOptionsDefault()
        {
            return _pivotApply == McPivotApply.MoveToPivot && _pivot == null && _pivotOffset == Vector3.zero &&
                   _pivotRotationOffset == Vector3.zero;
        }

        private void ClearPivotOptions()
        {
            _pivotApply = McPivotApply.MoveToPivot;
            _pivot = null;
            _pivotOffset = Vector3.zero;
            _pivotRotationOffset = Vector3.zero;
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPivot) return;

            var p = GetPivotWorldPosition();
            var r = GetPivotWorldRotation();

            var size = HandleUtility.GetHandleSize(p) * _pivotSphereSize;
            
            var oldZ = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            
            // Sphere
            Handles.color = Color.white;
            Handles.SphereHandleCap(0, p, Quaternion.identity, size * 0.2f, EventType.Repaint);
            
            // Axis Lines
            var xDir = r * Vector3.right;
            var yDir = r * Vector3.up;
            var zDir = r * Vector3.forward;

            Handles.color = Color.red;
            Handles.DrawLine(p, p + xDir * size);
            Handles.ConeHandleCap(0, p + xDir * size, r * Quaternion.LookRotation(Vector3.right), size * 0.15f, EventType.Repaint);

            Handles.color = Color.green;
            Handles.DrawLine(p, p + yDir * size);
            Handles.ConeHandleCap(0, p + yDir * size, r * Quaternion.LookRotation(Vector3.up), size * 0.15f, EventType.Repaint);

            Handles.color = Color.blue;
            Handles.DrawLine(p, p + zDir * size);
            Handles.ConeHandleCap(0, p + zDir * size, r * Quaternion.LookRotation(Vector3.forward), size * 0.15f, EventType.Repaint);

            Handles.color = Color.white;
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
            if (!_pivot) return _pivotOffset;

            // Offset in local space
            if (_localPivotOffset)
            {
                return _pivot.TransformPoint(_pivotOffset);
            }

            // Offset in world space
            return _pivot.position + _pivotOffset;
        }

        private Quaternion GetPivotWorldRotation()
        {
            var offset = Quaternion.Euler(_pivotRotationOffset);

            // If no pivot assigned, just use the offset as world rotation
            if (!_pivot)
                return offset;

            var baseRot = _pivot.rotation;

            // If is local then offset in local space
            if (_localPivotOffset)
                return baseRot * offset;

            // If is world then offset in world space
            return offset * baseRot;
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
            var combineArgs = new MeshCombinerTool.CombineArguments
            {
                MeshName = _fileName,
                MeshFilters = meshFilters,
                GetPosition = GetPivotWorldPosition,
                GetRotation = GetPivotWorldRotation,
                PivotApply = _pivotApply,
                MaterialOption = _material
            };
            
            if (MeshCombinerTool.TryCombineMesh(ref _meshFilter, ref _meshRenderer, combineArgs) != MeshCombinerTool.CombineResult.Successful)
            {
                Debug.LogError("ERROR: [MeshCombinerWindow] Couldn't combine the meshes");
            }
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
        }
        
        private void SaveMesh()
        {
            var filePath = _folderPath + "/" + _fileName + ".asset";
            AssetDatabase.CreateAsset(_meshFilter.sharedMesh, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Combined mesh saved at: " + filePath);
        }
    }
}
