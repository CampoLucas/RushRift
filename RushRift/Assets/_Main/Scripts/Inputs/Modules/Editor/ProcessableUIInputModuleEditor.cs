using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.InputSystem.UI
{
    [CustomEditor(typeof(ProcessableUIInputModule))]
    public class ProcessableUIInputModuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _point;
        private SerializedProperty _move;
        private SerializedProperty _submit;
        private SerializedProperty _cancel;
        private SerializedProperty _leftClick;
        private SerializedProperty _middleClick;
        private SerializedProperty _rightClick;
        private SerializedProperty _scrollWheel;
        private SerializedProperty _trackedPosition;
        private SerializedProperty _trackedOrientation;
        private SerializedProperty _cursorLockBehavior; 
        
        private MethodInfo _unassignActions;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _point = serializedObject.FindProperty("m_PointAction");
            _move = serializedObject.FindProperty("m_MoveAction");
            _submit = serializedObject.FindProperty("m_SubmitAction");
            _cancel = serializedObject.FindProperty("m_CancelAction");
            _leftClick = serializedObject.FindProperty("m_LeftClickAction");
            _middleClick = serializedObject.FindProperty("m_MiddleClickAction");
            _rightClick = serializedObject.FindProperty("m_RightClickAction");
            _scrollWheel = serializedObject.FindProperty("m_ScrollWheelAction");
            _trackedPosition = serializedObject.FindProperty("m_TrackedDevicePositionAction");
            _trackedOrientation = serializedObject.FindProperty("m_TrackedDeviceOrientationAction");
            
            // Try to get internal Cursor Lock Behaviour field if it exists
            _cursorLockBehavior = serializedObject.FindProperty("m_CursorLockBehavior");

            _unassignActions = typeof(InputSystemUIInputModule)
                .GetMethod("UnassignActions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public override void OnInspectorGUI()
        {
            Header("Input Actions");
            // Draw the default properties of the InputSystemUIInputModule
            base.OnInspectorGUI();
            
            
            
            serializedObject.Update();
            //EditorGUILayout.Space(15);
            
            // var module = (ProcessableUIInputModule)target;
            //
            // Header("Input Actions");
            //
            // // Editable Actions Asset
            // EditorGUI.BeginChangeCheck();
            // var newAsset = (InputActionAsset)EditorGUILayout.ObjectField(
            //     "Actions Asset", module.actionsAsset, typeof(InputActionAsset), false);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     Undo.RecordObject(module, "Change Input Actions Asset");
            //     module.actionsAsset = newAsset;
            //     
            //     // Set sensible defaults from new asset
            //     module.AssignDefaultActions();
            //     EditorUtility.SetDirty(module);
            // }
            
            // --- Force-editable Actions Asset ---
            EditorGUILayout.Space(10);
            Header("Input Actions (Override)");

            var module = (ProcessableUIInputModule)target;
            DrawActionsAssetMenu(module);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ActionsAsset"));

            EditorGUILayout.Space(5);

            // Draw each action as a popup sourced from the current asset
            DrawActionPopup("Point", _point, module);
            DrawActionPopup("Move", _move, module);
            DrawActionPopup("Submit", _submit, module);
            DrawActionPopup("Cancel", _cancel, module);
            DrawActionPopup("Left Click", _leftClick, module);
            DrawActionPopup("Middle Click", _middleClick, module);
            DrawActionPopup("Right Click", _rightClick, module);
            DrawActionPopup("Scroll Wheel", _scrollWheel, module);
            DrawActionPopup("Tracked Device Position", _trackedPosition, module);
            DrawActionPopup("Tracked Device Orientation", _trackedOrientation, module);

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign Default Actions"))
                {
                    Undo.RecordObject(module, "Assign Default Actions");
                    module.AssignDefaultActions();
                    EditorUtility.SetDirty(module);
                }

                if (GUILayout.Button("Unassign Actions"))
                {
                    Undo.RecordObject(module, "Unassign Actions");
                    _unassignActions?.Invoke(module, null);
                    EditorUtility.SetDirty(module);
                }
            }
            
            EditorGUILayout.Space(8);

            // --- Advanced foldout ---
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true, EditorStyles.foldoutHeader);
            if (_showAdvanced)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (_cursorLockBehavior != null)
                        EditorGUILayout.PropertyField(_cursorLockBehavior, new GUIContent("Cursor Lock Behaviour"));
                    else
                        EditorGUILayout.HelpBox("Cursor Lock Behaviour not found in this Unity version.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(12);

            // --- Processor Section ---
            Header("Processors");
            DrawProcessorsSection(module);

            serializedObject.ApplyModifiedProperties();

            serializedObject.ApplyModifiedProperties();
        }
        
        private static void Header(string text)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }
        
        /// <summary>
        /// Draws a popup for a SerializedProperty that stores an InputActionReference.
        /// Options are pulled from module.actionsAsset.actionMaps[*].actions[*].
        /// </summary>
        private void DrawActionPopup(string label, SerializedProperty actionRefProp, ProcessableUIInputModule module)
        {
            // Build list from the current asset
            var asset = module.actionsAsset;
            if (asset == null)
            {
                // If no asset, show a disabled label
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField(label, "<No Input Action Asset>");
                return;
            }

            var options = CacheActions(asset, out var ids);

            // Determine current selection
            var currentRef = actionRefProp.objectReferenceValue as InputActionReference;
            int currentIndex = 0; // 0 = <None>
            if (currentRef != null && currentRef.action != null)
            {
                var currentId = currentRef.action.id;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ids[i] == currentId) { currentIndex = i + 1; break; }
                }
            }

            // Draw popup
            int newIndex = EditorGUILayout.Popup(label, currentIndex, options);

            // Assign if changed
            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    actionRefProp.objectReferenceValue = null; // <None>
                }
                else
                {
                    var pickedId = ids[newIndex - 1];
                    var picked = FindActionById(asset, pickedId);
                    actionRefProp.objectReferenceValue = picked != null
                        ? InputActionReference.Create(picked)
                        : null;
                }
            }
        }

        /// <summary>
        /// Flattens all actions of the asset as "Map/Action" labels and returns both labels and corresponding IDs.
        /// </summary>
        private static string[] CacheActions(InputActionAsset asset, out List<System.Guid> actionIds)
        {
            var labels = new List<string> { "<None>" };
            actionIds = new List<System.Guid>();

            var maps = asset.actionMaps;
            for (int m = 0; m < maps.Count; m++)
            {
                var map = maps[m];
                var actions = map.actions;
                for (int a = 0; a < actions.Count; a++)
                {
                    var act = actions[a];
                    labels.Add($"{map.name}/{act.name}");
                    actionIds.Add(act.id);
                }
            }

            return labels.ToArray();
        }

        private static UnityEngine.InputSystem.InputAction FindActionById(InputActionAsset asset, System.Guid id)
        {
            var maps = asset.actionMaps;
            for (int m = 0; m < maps.Count; m++)
            {
                var actions = maps[m].actions;
                for (int a = 0; a < actions.Count; a++)
                {
                    if (actions[a].id == id)
                        return actions[a];
                }
            }
            return null;
        }
        
        #region Utility: Processor UI
        private void DrawProcessorsSection(ProcessableUIInputModule module)
        {
            var go = module.gameObject;
            var processors = go.GetComponents<UIInputProcessor>();

            if (processors.Length == 0)
            {
                EditorGUILayout.HelpBox("No UIInputProcessors attached.", MessageType.None);
            }
            else
            {
                foreach (var proc in processors)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(proc.GetType().Name, proc, typeof(UIInputProcessor), true);
                        if (GUILayout.Button("✕", GUILayout.Width(22)))
                        {
                            Undo.DestroyObjectImmediate(proc);
                            EditorUtility.SetDirty(module);
                            break;
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Add Processor", GUILayout.Height(22)))
            {
                ShowAddProcessorMenu(go);
            }
        }

        private void ShowAddProcessorMenu(GameObject go)
        {
            var menu = new GenericMenu();
            var processorType = typeof(UIInputProcessor);

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    processorType.IsAssignableFrom(t) &&
                    t.IsClass && !t.IsAbstract &&
                    typeof(MonoBehaviour).IsAssignableFrom(t) &&
                    (t.IsPublic || t.IsNestedPublic))
                .OrderBy(t => t.Name);

            foreach (var t in types)
            {
                menu.AddItem(new GUIContent(t.Name), false, () =>
                {
                    Undo.AddComponent(go, t);
                    EditorUtility.SetDirty(go);
                });
            }

            if (!types.Any())
                menu.AddDisabledItem(new GUIContent("No available UIInputProcessor classes"));

            menu.ShowAsContext();
        }
        #endregion

        #region Draw Input Action Asset

        private void DrawActionsAssetField(ProcessableUIInputModule module)
        {
            EditorGUILayout.BeginHorizontal();
        
            var newAsset = (InputActionAsset)EditorGUILayout.ObjectField(
                "Actions Asset", module.actionsAsset, typeof(InputActionAsset), false);
        
            if (GUILayout.Button("Pick…", GUILayout.MaxWidth(60)))
            {
                var path = EditorUtility.OpenFilePanel("Select Input Action Asset", "Assets", "inputactions");
                if (!string.IsNullOrEmpty(path))
                {
                    path = FileUtil.GetProjectRelativePath(path);
                    var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                    if (asset)
                    {
                        Undo.RecordObject(module, "Change Input Actions Asset");
                        module.SetActionsAsset(asset, false);
                        EditorUtility.SetDirty(module);
                    }
                }
            }
        
            if (GUILayout.Button("✕", GUILayout.MaxWidth(20)))
            {
                Undo.RecordObject(module, "Clear Input Actions Asset");
                module.SetActionsAsset(null, true);
                EditorUtility.SetDirty(module);
            }
        
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawActionsAssetMenu(ProcessableUIInputModule module)
        {
            EditorGUILayout.BeginHorizontal();

            // current asset display
            string assetName = module.actionsAsset ? module.actionsAsset.name : "<None>";
            EditorGUILayout.PrefixLabel("Actions Asset");
            if (GUILayout.Button(assetName, EditorStyles.layerMaskField))
            {
                ShowInputActionAssetMenu(module);
            }

            // small clear button
            if (GUILayout.Button("✕", GUILayout.MaxWidth(22)))
            {
                Undo.RecordObject(module, "Clear Actions Asset");
                module.actionsAsset = null;
                EditorUtility.SetDirty(module);
            }

            EditorGUILayout.EndHorizontal();
        }
        
        private void ShowInputActionAssetMenu(ProcessableUIInputModule module)
        {
            var menu = new GenericMenu();

            // find all InputActionAsset assets in the project
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            var assets = new List<InputActionAsset>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null) assets.Add(asset);
            }

            if (assets.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No InputActionAssets found"));
            }
            else
            {
                foreach (var asset in assets.OrderBy(a => a.name))
                {
                    bool isCurrent = module.actionsAsset == asset;
                    menu.AddItem(new GUIContent(asset.name), isCurrent, () =>
                    {
                        Undo.RecordObject(module, "Change Input Actions Asset");
                        module.actionsAsset = asset;
                        //module.AssignDefaultActions(); // refresh bindings
                        EditorUtility.SetDirty(module);
                    });
                }
            }

            menu.ShowAsContext();
        }

        #endregion
    }
}