// Assets/Editor/ReplaceWithPrefabWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    public class ReplaceWithPrefabWindow : EditorWindow
    {
        [Header("Prefab")]
        [SerializeField] private GameObject prefab;

        [Header("Options")]
        [SerializeField] private bool keepWorldTransform = true;
        [SerializeField] private bool keepName = true;
        [SerializeField] private bool keepLayerAndTag = true;
        [SerializeField] private bool keepActiveState = true;
        [SerializeField] private bool preserveStaticFlags = true;
        [SerializeField] private bool keepSiblingIndex = true;
        [SerializeField] private bool transferChildren = true;

        [MenuItem("Tools/Replace With Prefab…", priority = 200)]
        public static void ShowWindow()
        {
            var win = GetWindow<ReplaceWithPrefabWindow>("Replace With Prefab");
            win.minSize = new Vector2(380, 200);
            win.Show();
        }

        [MenuItem("GameObject/Replace With Prefab…", false, 0)]
        private static void ShowWindowFromHierarchy()
        {
            ShowWindow();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab", "Prefab to instantiate for each selected object"), prefab, typeof(GameObject), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            keepWorldTransform = EditorGUILayout.Toggle(new GUIContent("Keep World Transform", "Keeps world position/rotation/scale. If off, keeps local transform."), keepWorldTransform);
            keepSiblingIndex   = EditorGUILayout.Toggle(new GUIContent("Keep Sibling Index", "Keeps the same order in the hierarchy."), keepSiblingIndex);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            keepName          = EditorGUILayout.Toggle("Keep Name", keepName);
            keepLayerAndTag   = EditorGUILayout.Toggle("Keep Layer & Tag", keepLayerAndTag);
            keepActiveState   = EditorGUILayout.Toggle("Keep Active State", keepActiveState);
            preserveStaticFlags = EditorGUILayout.Toggle(new GUIContent("Preserve Static Flags", "Copies Static flags to the new instance."), preserveStaticFlags);
            transferChildren  = EditorGUILayout.Toggle(new GUIContent("Transfer Children", "Reparents old children under the new instance."), transferChildren);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!CanReplace()))
            {
                if (GUILayout.Button($"Replace Selected ({Selection.gameObjects.Length})"))
                {
                    ReplaceSelected();
                }
            }

            EditorGUILayout.HelpBox("Select scene objects in the Hierarchy, assign a Prefab, and click Replace. All operations are Undoable.", MessageType.Info);
        }

        private bool CanReplace()
        {
            if (!prefab) return false;
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.NotAPrefab) return false;

            // Any selected scene objects?
            foreach (var go in Selection.gameObjects)
                if (go && go.scene.IsValid()) return true;

            return false;
        }

        private void ReplaceSelected()
        {
            if (!CanReplace())
            {
                Debug.LogWarning("Replacement aborted: invalid prefab or no scene objects selected.");
                return;
            }

            // Group undo
            Undo.SetCurrentGroupName("Replace With Prefab");
            int undoGroup = Undo.GetCurrentGroup();

            var selected = new List<GameObject>();
            foreach (var go in Selection.gameObjects)
            {
                if (!go) continue;
                if (!go.scene.IsValid()) continue; // ignore assets/prefabs in project view
                selected.Add(go);
            }

            // Keep selection stable: replace parents first or last? Safer to process by depth descending
            // so children are not orphaned incorrectly when transferChildren = true.
            selected.Sort((a, b) => GetDepth(b.transform).CompareTo(GetDepth(a.transform)));

            var newSelection = new List<Object>(selected.Count);

            foreach (var old in selected)
            {
                if (!old) continue;

                var parent = old.transform.parent;
                int siblingIndex = old.transform.GetSiblingIndex();

                // Cache properties
                string oldName = old.name;
                int oldLayer = old.layer;
                string oldTag = old.tag;
                bool oldActive = old.activeSelf;
                var staticFlags = GameObjectUtility.GetStaticEditorFlags(old);

                // Cache transform (world or local)
                Vector3 pos, scale;
                Quaternion rot;

                if (keepWorldTransform)
                {
                    pos = old.transform.position;
                    rot = old.transform.rotation;
                    scale = old.transform.lossyScale;
                }
                else
                {
                    pos = old.transform.localPosition;
                    rot = old.transform.localRotation;
                    scale = old.transform.localScale;
                }

                // Cache children if we need to transfer them
                var children = transferChildren ? CacheChildren(old.transform) : null;

                // Create new instance in same scene & register undo
                var scene = old.scene;
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (!newObj)
                {
                    Debug.LogError("Failed to instantiate prefab. Aborting this object.");
                    continue;
                }
                Undo.RegisterCreatedObjectUndo(newObj, "Create replacement");

                // Parent first, then set transform
                Undo.SetTransformParent(newObj.transform, parent, "Set parent");
                if (keepSiblingIndex)
                    newObj.transform.SetSiblingIndex(siblingIndex);

                if (keepWorldTransform)
                {
                    newObj.transform.SetPositionAndRotation(pos, rot);
                    SetWorldScale(newObj.transform, scale);
                }
                else
                {
                    newObj.transform.localPosition = pos;
                    newObj.transform.localRotation = rot;
                    newObj.transform.localScale = scale;
                }

                // Transfer children
                if (transferChildren && children != null)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (!child) continue;
                        Undo.SetTransformParent(child, newObj.transform, "Transfer child");
                        child.SetSiblingIndex(i);
                    }
                }

                // Copy props
                if (keepName) newObj.name = oldName;
                if (keepLayerAndTag)
                {
                    newObj.layer = oldLayer;
                    try { newObj.tag = oldTag; } catch { /* tag might not exist */ }
                }
                if (keepActiveState) newObj.SetActive(oldActive);
                if (preserveStaticFlags) GameObjectUtility.SetStaticEditorFlags(newObj, staticFlags);

                newSelection.Add(newObj);

                // Remove the old one
                Undo.DestroyObjectImmediate(old);
            }

            // Reselect new instances
            Selection.objects = newSelection.ToArray();

            // Mark scenes dirty
            foreach (var obj in newSelection)
            {
                var go = obj as GameObject;
                if (go && go.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(go.scene);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static int GetDepth(Transform t)
        {
            int d = 0;
            while (t && t.parent) { d++; t = t.parent; }
            return d;
        }

        private static List<Transform> CacheChildren(Transform root)
        {
            var list = new List<Transform>(root.childCount);
            for (int i = 0; i < root.childCount; i++)
                list.Add(root.GetChild(i));
            return list;
        }

        private static void SetWorldScale(Transform t, Vector3 worldScale)
        {
            var parent = t.parent;
            if (!parent)
            {
                t.localScale = worldScale;
                return;
            }

            // Avoid divide-by-zero if parent scale contains zeros
            var p = parent.lossyScale;
            var x = Mathf.Approximately(p.x, 0f) ? 0f : worldScale.x / p.x;
            var y = Mathf.Approximately(p.y, 0f) ? 0f : worldScale.y / p.y;
            var z = Mathf.Approximately(p.z, 0f) ? 0f : worldScale.z / p.z;
            t.localScale = new Vector3(x, y, z);
        }
    }
}
#endif
