using UnityEditor;
using UnityEngine;

namespace Tools.Scripts.PropertyAttributes
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Saving previous GUI enabled value
            var previousGUIState = GUI.enabled;
            // Disabling edit for property
            GUI.enabled = false;
            // Drawing Property
            EditorGUI.PropertyField(position, property, label);
            // Setting old GUI enabled value
            GUI.enabled = previousGUIState;
        }
    }
    
    [CustomPropertyDrawer(typeof(ReadOnlyIfAttribute))]
    public class ReadOnlyConditionalDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ReadOnlyIfAttribute)attribute;
            var shouldBeEditable = GetBoolValue(property, attr.BoolFieldName, attr.Value);

            var previous = GUI.enabled;
            GUI.enabled = shouldBeEditable;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = previous;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool GetBoolValue(SerializedProperty property, string boolFieldName, bool value)
        {
            var boolProp = FindRelativeProperty(property, boolFieldName);

            if (boolProp == null)
            {
                Debug.LogWarning($"ReadOnlyIfFalse: Could not find bool field '{boolFieldName}' for '{property.name}'.");
                return true;
            }

            if (boolProp.propertyType != SerializedPropertyType.Boolean)
            {
                Debug.LogWarning($"ReadOnlyIfFalse: Field '{boolFieldName}' is not a bool.");
                return true;
            }

            return boolProp.boolValue != value;
        }

        private SerializedProperty FindRelativeProperty(SerializedProperty property, string fieldName)
        {
            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');

            if (lastDot >= 0)
            {
                var parentPath = path.Substring(0, lastDot);
                return property.serializedObject.FindProperty(parentPath + "." + fieldName);
            }

            return property.serializedObject.FindProperty(fieldName);
        }
    }

    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideConditionalDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            var shouldShow = GetBoolValue(property, attr.BoolFieldNames, attr.Value);

            if (!shouldShow)
                return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            var shouldShow = GetBoolValue(property, attr.BoolFieldNames, attr.Value);

            if (!shouldShow)
                return 0f;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool GetBoolValue(SerializedProperty property, string[] boolFieldNames, bool value)
        {
            for (var i = 0; i < boolFieldNames.Length; i++)
            {
                if (!GetBoolValue(property, boolFieldNames[i], value))
                {
                    return false;
                }
            }

            return true;
        }
        
        private bool GetBoolValue(SerializedProperty property, string boolFieldName, bool value)
        {
            var boolProp = FindRelativeProperty(property, boolFieldName);

            if (boolProp == null)
            {
                Debug.LogWarning($"HideIfFalse: Could not find bool field '{boolFieldName}' for '{property.name}'.");
                return true;
            }

            if (boolProp.propertyType != SerializedPropertyType.Boolean)
            {
                Debug.LogWarning($"HideIfFalse: Field '{boolFieldName}' is not a bool.");
                return true;
            }

            return boolProp.boolValue != value;
        }

        private SerializedProperty FindRelativeProperty(SerializedProperty property, string fieldName)
        {
            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');

            if (lastDot >= 0)
            {
                var parentPath = path.Substring(0, lastDot);
                return property.serializedObject.FindProperty(parentPath + "." + fieldName);
            }

            return property.serializedObject.FindProperty(fieldName);
        }
    }
}