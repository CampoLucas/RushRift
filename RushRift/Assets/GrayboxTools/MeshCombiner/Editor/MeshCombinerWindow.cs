using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GrayboxTools.MeshCombiner.Editor
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
        private bool _fromChildren = true;
        private bool _includeInactive = true;
        private bool _excludeTargetMesh = true;
        private bool _disableSources = true;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        
        // Pivot
        private McPivotApply _pivotApply = McPivotApply.MoveToPivot;
        private bool _localPivotOffset = true;
        private bool _sameAsTarget;
        private Transform _pivot;
        private Vector3 _pivotOffset;
        private Vector3 _pivotRotationOffset;

        private SubmeshOption _submeshOpt;
        private string _assetName = "Combined Mesh";
        private bool _useAssetPrefix = false;
        private string _assetPrefix = "M_";
        private string _folderPath = "Assets/__temp_combined_meshes";
        
        private CombineInstance[] _combine;
        
        private SerializedObject _serializedObject;
        private SerializedProperty _meshFiltersProp;
        
        private Vector2 _scroll;
        private bool _savedMesh;
        private bool _combinedMesh;
        private bool _triedToCombine;

        #region Debug

        private bool _showPivot = true;
        private float _pivotSize = 1f;
        private float _pivotArrowSize = .5f;
        private float _pivotArrowLength = .5f;
        private float _pivotSphereSize = .5f;

        #endregion

        [MenuItem("Tools/Graybox Tools/Mesh Combiner")]
        public static void Open()
        {
            var window = GetWindow<MeshCombinerWindow>("Mesh Combiner");
            window.SetTabIcon();
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _meshFiltersProp = _serializedObject.FindProperty("meshFilters");
            _triedToCombine = false;
            
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

        #region SetDirty Methods

        /// <summary>
        /// Marks active scene as modified
        /// </summary>
        private void MarkSceneDirty()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private void SetTargetsDirty(bool setTransform = true)
        {
            if (_meshFilter) EditorUtility.SetDirty(_meshFilter);
            if (_meshRenderer) EditorUtility.SetDirty(_meshRenderer);
            if (setTransform && _meshFilter) EditorUtility.SetDirty(_meshFilter.transform);
        }
        
        private void SetWindowDirty()
        {
            EditorUtility.SetDirty(this);
        }

        #endregion
        
        #region DrawOnGUI Methods

        private void OnGUI()
        {
            var pivot = GetPivot();
            
            _serializedObject.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(10);
            DrawTitleGUI();
            EditorGUILayout.Space(20);
            DrawTargetMeshGUI();
            EditorGUILayout.Space(10);
            DrawMeshesToCombineGUI();
            EditorGUILayout.Space(10);
            DrawPivotGUI(pivot);
            EditorGUILayout.Space(10);
            DrawSubmeshOptionsGUI();
            EditorGUILayout.Space(10);
            DrawOutputGUI();
            EditorGUILayout.Space(10);
            DrawButtonsGUI();

            EditorGUILayout.EndScrollView();
            _serializedObject.ApplyModifiedProperties();
            
            
        }

        private void DrawButtonsGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                //EditorGUI.BeginDisabledGroup(_meshFilter.IsNullOrMissing() || _meshRenderer.IsNullOrMissing() || meshFilters == null || meshFilters.Count == 0);
                if (GUILayout.Button("Combine"))
                {
                    _triedToCombine = true;
                    
                    CombineMeshes();
                }
                //EditorGUI.EndDisabledGroup();
                
                EditorGUI.BeginDisabledGroup(_meshFilter.IsNullOrMissing() || _meshRenderer.IsNullOrMissing());
                if (GUILayout.Button("Save"))
                {
                    SaveMesh();
                }
                EditorGUI.EndDisabledGroup();
            }

            if (MeshCombined() && !AssetSaved())
            {
                //EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("The combined mesh has not been saved yet!", MessageType.Warning, true);
            }
            
            // if (meshFilters.Count == 0)
            // {
            //     //EditorGUILayout.Space(5);
            //     EditorGUILayout.HelpBox("Haven't selected any meshes to combine yet.", MessageType.Warning, true);
            // }
            //
            // if (!_meshFilter || !_meshRenderer)
            // {
            //     //EditorGUILayout.Space(5);
            //     EditorGUILayout.HelpBox("The target Mesh Filter or MeshRenderer hasn't been assigned yet.", MessageType.Warning, true);
            // }
        }

        private void DrawOutputGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Output", EditorStyles.boldLabel);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                
                
                _assetName = EditorGUILayout.TextField("Asset Name", _assetName);
                if (GUILayout.Button("Auto", GUILayout.Width(70)))
                {
                    SetNameAsMeshFilter();
                }
                
                
            }
            
            using (var toggle = new EditorGUILayout.ToggleGroupScope("Use Prefix", _useAssetPrefix))
            {
                _useAssetPrefix = toggle.enabled;
                
                _assetPrefix = EditorGUILayout.TextField("Asset Prefix", _assetPrefix);
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
        }

        private void DrawSubmeshOptionsGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Submesh Options", EditorStyles.boldLabel);
            }

            _submeshOpt = (SubmeshOption)EditorGUILayout.EnumPopup("Combine Mode", _submeshOpt);

            var explanation = _submeshOpt switch
            {
                SubmeshOption.MergeAllSubmeshes =>
                    "Outputs one submesh and assigns the first material. Lowest draw calls, but you lose per material separation.",
                SubmeshOption.KeepAllSubmeshes =>
                    "Preserves each source submesh. Can create submeshes and increase draw calls.",
                SubmeshOption.MergeByMaterial =>
                    "Groups submeshes by shared material and outputs one submesh per unique material. Reduces draw calls compared to keeping all submeshes, but merged parts use the same material.",
                _ => null
            };

            if (!String.IsNullOrEmpty(explanation))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(explanation, MessageType.Info, true);
            }
        }
        
        private void DrawPivotGUI(Transform pivot)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Pivot Options", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(!SelectionIsGameObject() || _sameAsTarget);
                if (GUILayout.Button("Selection", EditorStyles.toolbarButton))
                {
                    GetPivotFromSelection();
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.BeginDisabledGroup(pivot == null);
                
                // icon + text that changes depending on current mode
                var spaceContent = pivot && _localPivotOffset
                    ? new GUIContent(" Local", _localSpaceIcon.image, "Offset is in pivot local space")
                    : new GUIContent(" Global", _globalSpaceIcon.image, "Offset is in global space");

                // Draw as a pressed/unpressed toolbar button
                if (!pivot)
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
                var gizmosDropIcon = EditorGUIUtility.IconContent("d_icon dropdown");
                if (gizmosDropIcon.image == null) gizmosDropIcon = EditorGUIUtility.IconContent("icon dropdown");

                if (GUILayout.Button(gizmosDropIcon, EditorStyles.toolbarDropDown, GUILayout.Width(18)))
                {
                    var popupPos = Event.current.mousePosition;
                    var anchor = new Rect(popupPos, Vector2.zero);
                    
                    PopupWindow.Show(anchor, new OptionsPopup(this, PivotGizmosOptions, new Vector2(300, 110)));
                }
                
                EditorGUI.BeginDisabledGroup(IsPivotOptionsDefault());
                if (GUILayout.Button(_clearIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ClearPivotOptions();
                }
                EditorGUI.EndDisabledGroup();
                
                
            }

            if (!_sameAsTarget)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("New Mesh Pivot", GUILayout.MaxWidth(LabelMaxWidth));
                    _pivot = (Transform)EditorGUILayout.ObjectField(GUIContent.none, _pivot, typeof(Transform), true, GUILayout.MinWidth(30));
                }   
            }
            _sameAsTarget = EditorGUILayout.Toggle("Same as target", _sameAsTarget);

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
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Pivot Positioning Mode", EditorStyles.boldLabel);
            _pivotApply = (McPivotApply)EditorGUILayout.EnumPopup("Mode", _pivotApply);
            var explanation = _pivotApply switch
            {
                McPivotApply.MoveToPivot =>
                    "Moves the target transform to the computed pivot while preserving children world positions when combining.",
                McPivotApply.KeepInPlace =>
                    "Does not move the target transform when combining.",
                _ => null
            };

            if (!String.IsNullOrEmpty(explanation))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(explanation, MessageType.Info, true);
            }
            
            
        }

        private void DrawTargetMeshGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Meshes Target", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                EditorGUI.BeginDisabledGroup(!SelectionIsGameObject());
                if (GUILayout.Button("Add And Select", EditorStyles.toolbarButton))
                {
                    AddTargetMeshToSelection();
                }
                if (GUILayout.Button("From Selection", EditorStyles.toolbarButton))
                {
                    GetTargetMeshFromSelection();
                }
                EditorGUI.EndDisabledGroup();
                
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

            if (_triedToCombine && (!_meshFilter || !_meshRenderer))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("The target Mesh Filter or MeshRenderer hasn't been assigned yet.", MessageType.Warning, true);
            }
        }

        private void DrawMeshesToCombineGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Meshes To Combine", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(!SelectionIsGameObject());
                if (GUILayout.Button("From Selection", EditorStyles.toolbarButton))
                {
                    GetMeshesFromSelection(_fromChildren, _includeInactive, _excludeTargetMesh);
                }
                EditorGUI.EndDisabledGroup();
                // dropdown arrow button
                var dropIcon = EditorGUIUtility.IconContent("d_icon dropdown");
                if (dropIcon.image == null) dropIcon = EditorGUIUtility.IconContent("icon dropdown");

                if (GUILayout.Button(dropIcon, EditorStyles.toolbarDropDown, GUILayout.Width(18)))
                {
                    var popupPos = Event.current.mousePosition;
                    var anchor = new Rect(popupPos, Vector2.zero);
                    
                    PopupWindow.Show(anchor, new OptionsPopup(this, SelectMeshesOptions, new Vector2(260, 90)));
                }
                
                
                EditorGUI.BeginDisabledGroup(meshFilters == null || meshFilters.Count == 0);
                if (GUILayout.Button(_clearIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ClearMeshes();
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.PropertyField(_meshFiltersProp, true);
            _disableSources = EditorGUILayout.Toggle("Disable Meshes After", _disableSources);

            if (_triedToCombine && (meshFilters == null || meshFilters.Count == 0))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Haven't selected any meshes to combine yet.", MessageType.Warning, true);
            }
            
        }

        private void DrawTitleGUI()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 35
            };
            EditorGUILayout.LabelField(titleContent.text, titleStyle, GUILayout.MinHeight(30));
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPivot) return;

            var p = GetPivotWorldPosition();
            var r = GetPivotWorldRotation();

            var handleSize = HandleUtility.GetHandleSize(p);
            var sphereSize = handleSize * GetSphereSize();
            
            var oldZ = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            
            // Sphere
            Handles.color = Color.white;
            Handles.SphereHandleCap(0, p, Quaternion.identity, sphereSize * 0.2f, EventType.Repaint);
            
            // Axis Lines
            var xDir = r * Vector3.right;
            var yDir = r * Vector3.up;
            var zDir = r * Vector3.forward;

            Handles.color = Color.red;
            var arrowSize = handleSize * GetArrowSize();
            var arrowLength = handleSize * GetArrowLength();
            Handles.DrawLine(p, p + xDir * arrowLength);
            Handles.ConeHandleCap(0, p + xDir * arrowLength, r * Quaternion.LookRotation(Vector3.right), arrowSize * 0.15f, EventType.Repaint);

            Handles.color = Color.green;
            Handles.DrawLine(p, p + yDir * arrowLength);
            Handles.ConeHandleCap(0, p + yDir * arrowLength, r * Quaternion.LookRotation(Vector3.up), arrowSize * 0.15f, EventType.Repaint);

            Handles.color = Color.blue;
            Handles.DrawLine(p, p + zDir * arrowLength);
            Handles.ConeHandleCap(0, p + zDir * arrowLength, r * Quaternion.LookRotation(Vector3.forward), arrowSize * 0.15f, EventType.Repaint);

            Handles.color = Color.white;
            Handles.Label(p, "Pivot");
            
            Handles.zTest = oldZ;
        }
        
        public void SelectMeshesOptions(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Select Meshes Settings", EditorStyles.boldLabel);
            }
            EditorGUILayout.Space(5);

            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Exclude target mesh");
                _excludeTargetMesh = EditorGUILayout.Toggle(GUIContent.none, _excludeTargetMesh, GUILayout.Width(15));
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("From children");
                _fromChildren = EditorGUILayout.Toggle(GUIContent.none, _fromChildren, GUILayout.Width(15));
            }
            
            EditorGUI.BeginDisabledGroup(!_fromChildren);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Include inactive");
                _includeInactive = EditorGUILayout.Toggle(GUIContent.none, _includeInactive, GUILayout.Width(15));
            }
            EditorGUI.EndDisabledGroup();
        }
        
        public void PivotGizmosOptions(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Gizmos Settings", EditorStyles.boldLabel);
            }
            EditorGUILayout.Space(5);

            _pivotSize = EditorGUILayout.Slider("Marker Scale", _pivotSize, 0.01f, 2f);
            _pivotSphereSize = EditorGUILayout.Slider("Sphere Radius", _pivotSphereSize, 0.01f, 2f);
            _pivotArrowLength = EditorGUILayout.Slider("Arrows Length", _pivotArrowLength, 0.01f, 2f);
            _pivotArrowSize = EditorGUILayout.Slider("Arrows Head Size", _pivotArrowSize, 0.01f, 2f);
        }

        #endregion

        private float GetSphereSize() => _pivotSphereSize * _pivotSize;
        private float GetArrowSize() => _pivotArrowSize * _pivotSize;
        private float GetArrowLength() => _pivotArrowLength * _pivotSize;

        private Transform GetPivot()
        {
            return _sameAsTarget ? _meshFilter ? _meshFilter.transform : null : _pivot;
        }

        private bool SelectionIsGameObject()
        {
            return Selection.activeGameObject == true;
        }
        
        private bool IsPivotOptionsDefault()
        {
            return _pivotApply == McPivotApply.MoveToPivot && _pivot == null && _pivotOffset == Vector3.zero &&
                   _pivotRotationOffset == Vector3.zero && _sameAsTarget == false;
        }

        private void ClearPivotOptions()
        {
            _pivotApply = McPivotApply.MoveToPivot;
            _pivot = null;
            _pivotOffset = Vector3.zero;
            _pivotRotationOffset = Vector3.zero;
            _sameAsTarget = false;
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
            _combinedMesh = false;
        }

        private void GetMeshesFromSelection(bool fromChildren = true, bool includeInactive = true, bool excludeTargetMesh = false)
        {
            ClearMeshes();
            
            if (fromChildren)
            {
                foreach (var gameObject in Selection.gameObjects)
                {
                    var filters = gameObject.GetComponentsInChildren<MeshFilter>(includeInactive);
                    meshFilters.AddRange(filters);
                }
            }
            else
            {
                foreach (var gameObject in Selection.gameObjects)
                {
                    var filter = gameObject.GetComponent<MeshFilter>();
                    if (filter)
                    {
                        meshFilters.Add(filter);
                    }
                }
            }

            if (excludeTargetMesh && !_meshFilter.IsNullOrMissing() && meshFilters.Contains(_meshFilter))
            {
                meshFilters.Remove(_meshFilter);
            }
        }

        private void GetTargetMeshFromSelection()
        {
            _meshFilter = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<MeshFilter>() : null;
            _meshRenderer = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<MeshRenderer>() : null;
        }

        private void AddTargetMeshToSelection()
        {
            var active = Selection.activeGameObject;

            var hasFilter = active.TryGetComponent<MeshFilter>(out var filter);
            var hasRenderer = active.TryGetComponent<MeshRenderer>(out var renderer);
            
            if ((!hasFilter || !hasRenderer) && !EditorUtility.DisplayDialog(
                    "Add MeshFilter and MeshRenderer", 
                    $"You are about to add a MeshFilter and MeshRenderer to {active.name}.\n\nAre you sure?", "Yes",
                    "Cancel"))
            {
                return;
            }
            
            if (!active)
            {
                return;
            }
            if (!hasFilter)
            {
                filter = active.AddComponent<MeshFilter>();
            }

            _meshFilter = filter;

            if (!hasRenderer)
            {
                renderer = active.AddComponent<MeshRenderer>();
            }

            _meshRenderer = renderer;

            if (!hasFilter || !hasRenderer)
            {
                EditorUtility.SetDirty(active);
            }
        }

        private void GetPivotFromSelection()
        {
            var p = Selection.activeGameObject ? Selection.activeGameObject.transform : null;
            if (p) _pivot = p;
        }
        
        private Vector3 GetPivotWorldPosition()
        {
            var pivot = GetPivot();
            
            if (!pivot) return _pivotOffset;

            // Offset in local space
            if (_localPivotOffset)
            {
                return pivot.TransformPoint(_pivotOffset);
            }

            // Offset in world space
            return pivot.position + _pivotOffset;
        }

        private Quaternion GetPivotWorldRotation()
        {
            var pivot = GetPivot();
            var offset = Quaternion.Euler(_pivotRotationOffset);

            // If no pivot assigned, just use the offset as world rotation
            if (!pivot)
                return offset;

            var baseRot = pivot.rotation;

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

        private void SetNameAsMeshFilter()
        {
            var name = _meshFilter ? _meshFilter.gameObject.name : "Combined_Mesh";
            _assetName = $"{name}";
        }
        
        private void PickFolderInsideAssets()
        {
            var abs = EditorUtility.OpenFolderPanel("Pick output folder (inside Assets)", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            abs = abs.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            if (!abs.StartsWith(dataPath))
            {
                EditorUtility.DisplayDialog("Couldn't select folder.", "Folder must be inside this project's Assets folder.", "OK");
                Debug.LogError("ERROR: [MeshCombiner] Folder must be inside this project's Assets folder.");
                return;
            }

            _folderPath = "Assets" + abs.Substring(dataPath.Length);
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
        }

        private string GetAssetName() => _useAssetPrefix ? _assetPrefix + _assetName : _assetName;

        private void CombineMeshes()
        {
            if (!_meshRenderer || !_meshFilter || meshFilters == null || meshFilters.Count == 0)
            {
                return;
            }
            
            var input = new CombineRequest($"{GetAssetName()} (Not Saved)", meshFilters, GetPivotWorldPosition(), GetPivotWorldRotation(),
                _submeshOpt);

            if (!MeshCombiner.TryCombineToTarget(_meshFilter, _meshRenderer, input, _pivotApply, _disableSources, out var error))
            {
                EditorUtility.DisplayDialog("Couldn't combine meshes.", error, "OK");
                Debug.LogError($"ERROR: [MeshCombiner] {error}");
                
                return;
            }

            _triedToCombine = false;
            _savedMesh = false;
            _combinedMesh = true;
            
            SetTargetsDirty();
            SetWindowDirty();
            MarkSceneDirty();
        }

        private void EnsureFolderExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        
        private void SaveMesh()
        {
            
            if (_meshFilter.sharedMesh.IsNullOrMissing())
            {
                EditorUtility.DisplayDialog("Couldn't Save Asset", "The mesh is null or missing", "OK");
                return;
            }
            var filePath = _folderPath + "/" + GetAssetName() + ".asset";
            EnsureFolderExists(_folderPath);
            
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(filePath);
            if (existing != null)
            {
                var option = ConfirmOverwrite(existing);

                if (option == 2)
                {
                    return;
                }

                if (option == 1)
                {
                    filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                }
            }

            var savedMesh = Instantiate(_meshFilter.sharedMesh);
            savedMesh.name = _assetName;
            
            AssetDatabase.CreateAsset(savedMesh, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _meshFilter.sharedMesh = savedMesh;
            
            _savedMesh = true;
            _combinedMesh = false;

            EditorUtility.SetDirty(savedMesh);
            SetTargetsDirty();
            SetWindowDirty();
            MarkSceneDirty();
            
            Debug.Log("Combined mesh saved at: " + filePath);
        }

        private bool AssetSaved()
        {
            return _savedMesh;
        }

        private bool MeshCombined()
        {
            return _combinedMesh;
        }
        
        private int ConfirmOverwrite(Object assetToOverwrite)
        {
            if (assetToOverwrite == null) return 0;

            var path = AssetDatabase.GetAssetPath(assetToOverwrite);
            if (string.IsNullOrEmpty(path)) return 0; // not an asset on disk

            // If the asset file exists, ask
            if (File.Exists(path))
            {
                return EditorUtility.DisplayDialogComplex(
                    "Override Asset",
                    $"\"{path}\" already exists.\n\nDo you want to overwrite it?",
                    "OK",
                    "Rename",
                    "Cancel"
                );
            }

            return 0;
        }
        
        private void SetTabIcon()
        {
            var icon = Resources.Load<Texture2D>("MeshCombiner/icon");

            if (!icon) return;
            titleContent = new GUIContent("Mesh Combiner", icon);
            Repaint();
        }
    }
}
