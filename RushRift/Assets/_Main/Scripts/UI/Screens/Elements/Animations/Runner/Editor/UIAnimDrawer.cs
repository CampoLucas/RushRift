using Game.UI.Animations;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UIAnim))]
public class UIAnimDrawer : PropertyDrawer
{
    private const float Padding = 2f; // small padding between fields

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;

        var typeProp = property.FindPropertyRelative("type");
        if (typeProp != null)
        {
            // Type field height
            height += EditorGUI.GetPropertyHeight(typeProp) + Padding;

            // Duration
            var durationProp = property.FindPropertyRelative("duration");
            if (durationProp != null)
                height += EditorGUI.GetPropertyHeight(durationProp) + Padding;

            // Delay
            var delayProp = property.FindPropertyRelative("delay");
            if (delayProp != null)
                height += EditorGUI.GetPropertyHeight(delayProp) + Padding;

            // Sub-property depending on type
            SerializedProperty subProp = GetSubProperty(property, typeProp);
            if (subProp != null)
                height += EditorGUI.GetPropertyHeight(subProp, true) + Padding;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var originalPosition = position;

        var typeProp = property.FindPropertyRelative("type");
        if (typeProp == null) return;

        // Draw Type
        float typeHeight = EditorGUI.GetPropertyHeight(typeProp);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, typeHeight), typeProp);
        position.y += typeHeight + Padding;

        // Draw Duration
        var durationProp = property.FindPropertyRelative("duration");
        if (durationProp != null)
        {
            float durHeight = EditorGUI.GetPropertyHeight(durationProp);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, durHeight), durationProp);
            position.y += durHeight + Padding;
        }

        // Draw Delay
        var delayProp = property.FindPropertyRelative("delay");
        if (delayProp != null)
        {
            float delayHeight = EditorGUI.GetPropertyHeight(delayProp);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, delayHeight), delayProp);
            position.y += delayHeight + Padding;
        }

        // Draw sub-property
        SerializedProperty subPropField = GetSubProperty(property, typeProp);
        if (subPropField != null)
        {
            float subHeight = EditorGUI.GetPropertyHeight(subPropField, true);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, subHeight), subPropField, true);
        }
    }

    private SerializedProperty GetSubProperty(SerializedProperty property, SerializedProperty typeProp)
    {
        UIAnimType type = (UIAnimType)typeProp.enumValueIndex;
        return type switch
        {
            UIAnimType.Move => property.FindPropertyRelative("moveAnim"),
            UIAnimType.Scale => property.FindPropertyRelative("scaleAnim"),
            UIAnimType.Rotate => property.FindPropertyRelative("rotationAnim"),
            UIAnimType.Color => property.FindPropertyRelative("colorAnim"),
            _ => null,
        };
    }
}