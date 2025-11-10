using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomPropertyDrawer(typeof(SerializableSO), true)]
    [CustomPropertyDrawer(typeof(SerializableSOAttribute), true)]
    public class PropertySODrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // if collapsed or null return just the object field line
            if (property.objectReferenceValue == null || !property.isExpanded)
                return EditorGUIUtility.singleLineHeight + 4;

            // Measure default inspector fields of the SO (no custom editor)
            var so = new SerializedObject(property.objectReferenceValue);
            so.Update();

            var h = EditorGUIUtility.singleLineHeight + 6; // the object field row + padding

            var it = so.GetIterator();
            var enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (it.propertyPath == "m_Script") continue; // skip script reference

                h += EditorGUI.GetPropertyHeight(it, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            // optional warning line
            h += EditorGUIUtility.singleLineHeight + 6;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;

            // foldout + object field on the same row
            var foldRect  = new Rect(position.x, position.y, 14, line);
            var fieldRect = new Rect(position.x + 16, position.y, position.width - 16, line);

            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, GUIContent.none);
            EditorGUI.PropertyField(fieldRect, property, label);

            if (!property.isExpanded) return;

            // null state
            if (property.objectReferenceValue == null)
            {
                var nullRect = new Rect(position.x + 16, position.y + line + 4, position.width - 16, line);
                EditorGUI.LabelField(nullRect, "The object reference is null.");
                return;
            }

            // draw the SO default inspector (no GUILayout, no custom Editor)
            var y = position.y + line + 6;
            var so = new SerializedObject(property.objectReferenceValue);
            so.Update();

            var it = so.GetIterator();
            var enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (it.propertyPath == "m_Script") continue;

                var ph = EditorGUI.GetPropertyHeight(it, true);
                var r = new Rect(position.x + 16, y, position.width - 16, ph);
                EditorGUI.PropertyField(r, it, true);
                y += ph + EditorGUIUtility.standardVerticalSpacing;
            }

            so.ApplyModifiedProperties();

            // warning (inside the property’s rect, not below)
            var warnRect = new Rect(position.x + 16, y + 2, position.width - 32, line);
            EditorGUI.HelpBox(warnRect,
                "Edits here modify the original ScriptableObject asset.",
                MessageType.Info);
        }
    }
}
