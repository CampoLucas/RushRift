using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.StateMachine.Interfaces;
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
            //base.OnInspectorGUI();
            
            serializedObject.Update();

            if (_collection is UIStatesOverride overrideAsset)
            {
                DrawBaseReference(overrideAsset);
                EditorGUILayout.Space(5);

                if (overrideAsset.Parent)
                {
                    DrawPresentersSection();
                }
                else
                {
                    EditorGUILayout.HelpBox("Needs a parent", MessageType.Error);
                }
            }
            else
            {
                DrawPresentersSection();
                EditorGUILayout.Space(5);

                DrawTransitionsSection();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawBaseReference(UIStatesOverride overrideAsset)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parent"));
        }

        private void DrawPresentersSection()
        {
            EditorGUILayout.LabelField("Presenters", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_presenters);
        }
        
        private void DrawTransitionsSection()
        {
            if (_transitionsList == null) return;

            EditorGUILayout.Space(5);
            _transitionsList.DoLayoutList();
        }

        private readonly Dictionary<int, bool> _foldoutStates = new();
        
        private void SetUpTransitionsList()
        {
            _transitionsList = new ReorderableList(serializedObject, _transitions, true, true, true, true);

            _transitionsList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Transitions");
            };

            _transitionsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = _transitions.GetArrayElementAtIndex(index);
                var from = element.FindPropertyRelative("from");
                var to = element.FindPropertyRelative("to");
                var conditions = element.FindPropertyRelative("conditions");

                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;
                var y = rect.y + 2;

                // Foldout toggle
                if (!_foldoutStates.ContainsKey(index))
                    _foldoutStates[index] = false;
                
                // Entire element box
                EditorGUI.BeginProperty(rect, GUIContent.none, element);

                var x = rect.x + 8;
                var width = 15 - 8;
                var foldoutRect = new Rect(x, y, width, lineHeight);
                _foldoutStates[index] = EditorGUI.Foldout(foldoutRect, _foldoutStates[index], GUIContent.none);
                
                
                // From / To side-by-side
                // var fromRect = new Rect(rect.x + 18, y, (rect.width - 20) * 0.48f, lineHeight);
                // var toRect = new Rect(rect.x + 18 + (rect.width - 20) * 0.52f, y, (rect.width - 20) * 0.48f, lineHeight);

                var popupWidth = (rect.width - 24) / 2f - 2f;
                var fromRect = new Rect(foldoutRect.xMax + 2f, y, popupWidth, lineHeight);
                var toRect = new Rect(fromRect.xMax + 4f, y, popupWidth, lineHeight);
                
                
                EditorGUI.BeginProperty(fromRect, GUIContent.none, from);
                EditorGUI.PropertyField(fromRect, from, GUIContent.none);
                EditorGUI.EndProperty();

                EditorGUI.BeginProperty(toRect, GUIContent.none, to);
                EditorGUI.PropertyField(toRect, to, GUIContent.none);
                EditorGUI.EndProperty();
                
                // Draw expanded conditions
                if (_foldoutStates[index])
                {
                    var condHeight = EditorGUI.GetPropertyHeight(conditions, true);
                    var condRect = new Rect(rect.x + 20, y + lineHeight + spacing, rect.width - 24, condHeight);

                    // This keeps the SerializedSOCollection drawn INSIDE the element
                    EditorGUI.BeginProperty(condRect, GUIContent.none, conditions);
                    EditorGUI.PropertyField(condRect, conditions, true);
                    EditorGUI.EndProperty();
                }
                
                EditorGUI.EndProperty();
            };

            _transitionsList.elementHeightCallback = index =>
            {
                var element = _transitions.GetArrayElementAtIndex(index);
                var conditions = element.FindPropertyRelative("conditions");

                var baseHeight = EditorGUIUtility.singleLineHeight + 6;
                if (_foldoutStates.TryGetValue(index, out bool expanded) && expanded)
                {
                    baseHeight += EditorGUI.GetPropertyHeight(conditions, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                return baseHeight;
            };
            
            // Handle removing and cleaning predicates
            _transitionsList.onRemoveCallback = list =>
            {
                var def = GetTransitionAtIndex(list.index);
                if (def != null)
                    def.DestroyPredicates();

                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                EditorUtility.SetDirty(_collection);
                AssetDatabase.SaveAssets();
            };

            // On add, create empty transition
            _transitionsList.onAddCallback = list =>
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
            };
            
        }
        
        private UITransitionDefinition GetTransitionAtIndex(int index)
        {
            if (_collection == null) return null;
            var list = _collection.Transitions;
            if (list == null || index < 0 || index >= list.Count)
                return null;

            return list[index];
        }
    }
}