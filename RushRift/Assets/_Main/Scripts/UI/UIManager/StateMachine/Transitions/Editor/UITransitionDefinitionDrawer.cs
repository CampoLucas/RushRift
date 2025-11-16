using UnityEditor;
using UnityEngine;

namespace Game.UI.StateMachine.Editor
{
    [CustomPropertyDrawer(typeof(UITransitionDefinition))]
    public class UITransitionDefinitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var conditions = property.FindPropertyRelative("conditions");

            // When collapsed, only one line (header row)
            if (!property.isExpanded || conditions == null)
                return line + 2;

            // When expanded, add conditions height
            return line + 2 + EditorGUI.GetPropertyHeight(conditions, true) + 6;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var from = property.FindPropertyRelative("from");
            var screenTransition = property.FindPropertyRelative("screenTransition");
            var to = property.FindPropertyRelative("to");
            var scene = property.FindPropertyRelative("scene");
            var conditions = property.FindPropertyRelative("conditions");

            var y = position.y;
            var x = position.x;
            var fullWidth = position.width;

            // --- Foldout (added) ---
            var foldoutRect = new Rect(x + 12f, y, 14f, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

            // Shift content to the right to make room for the foldout
            var contentX = x + 16f;
            var contentWidth = fullWidth - 16f;
            
            var fromRect = new Rect(contentX, y, contentWidth * 0.47f, lineHeight);
            var destRect = new Rect(contentX + contentWidth * 0.45f + 13, y, contentWidth * 0.47f, lineHeight);
            var buttonRect = new Rect(contentX + (contentWidth * 0.45f) * 2 + 26, y, 22, lineHeight);

            // Draw from
            EditorGUI.PropertyField(fromRect, from, GUIContent.none);

            // Draw TO or SCENE (unchanged)
            if (screenTransition.boolValue)
                EditorGUI.PropertyField(destRect, to, GUIContent.none);
            else
                EditorGUI.PropertyField(destRect, scene, GUIContent.none);

            // Draw your existing "…" button (unchanged)
            if (GUI.Button(buttonRect, "…"))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("To Screen"), screenTransition.boolValue, () =>
                {
                    screenTransition.boolValue = true;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.AddItem(new GUIContent("To Scene"), !screenTransition.boolValue, () =>
                {
                    screenTransition.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.DropDown(buttonRect);
            }

            // Draw CONDITIONS only when expanded (added)
            if (property.isExpanded && conditions != null)
            {
                var condY = y + lineHeight + spacing + 2;
                var condRect = new Rect(contentX, condY, contentWidth, EditorGUI.GetPropertyHeight(conditions, true));
                EditorGUI.PropertyField(condRect, conditions, true);
            }

            EditorGUI.EndProperty();
        }
    }
}