using System;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.Editor
{
    [CustomEditor(typeof(GameModeSO), true)]
    public class GameModeSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _singleLevel;
        private SerializedProperty _nextLevel;
        private SerializedProperty _nextGameMode;
        
        private void OnEnable()
        {
            _singleLevel = serializedObject.FindProperty("singleLevel");
            _nextLevel = serializedObject.FindProperty("nextLevel");
            _nextGameMode = serializedObject.FindProperty("nextGameMode");
        }

        public override void OnInspectorGUI()
        {
#if false
            base.OnInspectorGUI();
            
            serializedObject.Update();

            var gm = target as GameModeSO;
            var currSelection = gm.GetNextIsSingleLevel() ? 0 : 1;
            EditorGUILayout.LabelField("Next Level when finished");
            var selection = EditorGUILayout.MaskField(currSelection, new []
            {
                "Single Level",
                "Game Mode"
            });
            
            gm.SetNextIsSingleLevel(selection == 0);

            if (gm.GetNextIsSingleLevel())
            {
                EditorGUILayout.PropertyField(_level);
            }
            else
            {
                EditorGUILayout.PropertyField(_gameMode);
            }

            serializedObject.ApplyModifiedProperties();
#else
            serializedObject.Update();

            // Draw all other fields *except* the hidden ones
            DrawPropertiesExcluding(serializedObject, "m_Script", "singleLevel", "nextLevel", "nextGameMode");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Next On Completion", EditorStyles.boldLabel);

            // --- Dropdown ---
            var currentIndex = _singleLevel.boolValue ? 0 : 1;
            var newIndex = EditorGUILayout.Popup("Next Type", currentIndex, new[] { "Single Level", "Game Mode" });
            _singleLevel.boolValue = newIndex == 0;

            // --- Conditional field ---
            if (_singleLevel.boolValue)
            {
                EditorGUILayout.PropertyField(_nextLevel, new GUIContent("Next Level"));
            }
            else
            {
                EditorGUILayout.PropertyField(_nextGameMode, new GUIContent("Next Game Mode"));
            }

            serializedObject.ApplyModifiedProperties();
#endif
        }
    }
}