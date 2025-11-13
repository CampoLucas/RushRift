using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.UI.StateMachine.Interfaces;
using MyTools.Global;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Game.UI.StateMachine.Editor
{
    [CustomEditor(typeof(UIStateCollection), true)]
    public class UIStatesCollectionEditor : UnityEditor.Editor
    {
        private SerializedProperty _presenters;
        private SerializedProperty _transitions;
        private UIStateCollection _collection;
        private ReorderableList _transitionsList;

        private void OnEnable()
        {
            _collection = (UIStateCollection)target;
            _presenters = serializedObject.FindProperty("presenters");
            _transitions = serializedObject.FindProperty("transitions");
            
            if (_transitions != null)
                SetUpTransitionsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_collection is UIStatesOverride overrideAsset)
            {
                DrawBaseReference();
                EditorGUILayout.Space(5);

                if (overrideAsset.Parent)
                {
                    DrawParentPresentersSection();
                    EditorGUILayout.Space(5);
                }
                else
                {
                    EditorGUILayout.HelpBox("Needs a parent", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Root Screen");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("root"), GUIContent.none);
                
                DrawPresentersSection();
                EditorGUILayout.Space(5);
                DrawTransitionsSection();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawBaseReference()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parent"));
        }

        private void DrawPresentersSection()
        {
            EditorGUILayout.LabelField("Presenters", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_presenters);
            EditorGUI.indentLevel--;
        }
        
        private int FindKeyIndex(SerializedProperty dictionaryProp, UIScreen key)
        {
            var keysProp = dictionaryProp.FindPropertyRelative("keys");
            for (int i = 0; i < keysProp.arraySize; i++)
            {
                var keyProp = keysProp.GetArrayElementAtIndex(i);

                // For enums, compare the actual enum value
                if (keyProp.propertyType == SerializedPropertyType.Enum &&
                    keyProp.enumValueIndex == (int)(object)key)
                    return i;
            }

            return -1;
        }

        private void DrawParentPresentersSection()
        {
            if (_collection is not UIStatesOverride overrideAsset)
                return;

            var parent = overrideAsset.Parent;
            if (parent == null)
            {
                EditorGUILayout.HelpBox("Missing parent reference.", MessageType.Warning);
                return;
            }

            var parentDict = parent.GetPresenters();
            var overrideDict = GetDictionary(_collection, "presenters");

            if (overrideDict == null)
            {
                EditorGUILayout.HelpBox("Parent presenters dictionary not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Presenters", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Original", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
                GUILayout.Label("Override", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
            }

            foreach (var kvp in parentDict)
            {
                var screen = kvp.Key;
                var parentPresenter = kvp.Value;
                var overridePresenter = overrideAsset.Presenters.TryGetValue(screen, out var p) ? p : null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // read-only original
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(parentPresenter, typeof(BaseUIPresenter), false,
                            GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
                    }

                    // editable override
                    var newPresenter = (BaseUIPresenter)EditorGUILayout.ObjectField(
                        overridePresenter,
                        typeof(BaseUIPresenter),
                        false,
                        GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));

                    if (newPresenter != overridePresenter)
                    {
                        // Apply directly to the serialized property instead of runtime dictionary
                        Undo.RecordObject(_collection, "Assign UI Presenter Override");
                        overrideAsset.Presenters[screen] = newPresenter;
                        EditorUtility.SetDirty(_collection);
                    }
                }

                // Separator
                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth * 0.9f, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));
            }
        }
        
        private void DrawTransitionsSection()
        {
            if (_transitionsList == null) return;

            EditorGUILayout.Space(5);
            _transitionsList.DoLayoutList();
        }

        #region Transition

        private void SetUpTransitionsList()
        {
            _transitionsList = new ReorderableList(serializedObject, _transitions, true, true, true, true)
            {
                drawHeaderCallback = r => DrawHeaderHandler(r),
                drawElementCallback = (r, i, a, f) => DrawElementHandler(r, i),
                elementHeightCallback = ElementHeightHandler,
                onRemoveCallback = RemoveHandler,
                onAddCallback = AddHandler,
            };
        }

        void DrawHeaderHandler(Rect rect)
        {
            EditorGUI.LabelField(rect, "Transitions");
        }

        void DrawElementHandler(Rect rect, int index)
        {
            var element = _transitions.GetArrayElementAtIndex(index);

            rect.y += 2;
            rect.height = EditorGUI.GetPropertyHeight(element, true);

            EditorGUI.PropertyField(rect, element, GUIContent.none, true);
        }

        float ElementHeightHandler(int index)
        {
            var element = _transitions.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4;
        }

        void RemoveHandler(ReorderableList list)
        {
            var def = GetTransitionAtIndex(list.index);
            if (def != null)
                def.DestroyPredicates();

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            EditorUtility.SetDirty(_collection);
            AssetDatabase.SaveAssets();
        }

        void AddHandler(ReorderableList list)
        {
            serializedObject.Update();

            // Increase array
            _transitions.arraySize++;
            serializedObject.ApplyModifiedProperties();

            // Get the new element
            var newElement = _transitions.GetArrayElementAtIndex(_transitions.arraySize - 1);

            // Reset the collection (not via objectReferenceValue!)
            var conditions = newElement.FindPropertyRelative("conditions");
            if (conditions != null)
            {
                var innerList = conditions.FindPropertyRelative("collection");
                if (innerList != null)
                {
                    innerList.ClearArray(); // wipe any copied elements
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_collection);
        }

        #endregion

        #region Helpers

        private UITransitionDefinition GetTransitionAtIndex(int index)
        {
            if (_collection == null) return null;
            var list = _collection.GetTransitions();
            if (list == null || index < 0 || index >= list.Count)
                return null;

            return list[index];
        }
        
        private Dictionary<UIScreen, BaseUIPresenter> GetDictionary(UIStateCollection collection, string fieldName)
        {
            var field = typeof(UIStateCollection)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                return null;

            if (field.GetValue(collection) is SerializedDictionary<UIScreen, BaseUIPresenter> dict)
                return dict.GetDictionary();

            return null;
        }

        #endregion
    }
}