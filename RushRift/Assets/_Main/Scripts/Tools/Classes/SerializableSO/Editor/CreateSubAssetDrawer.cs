#if UNITY_EDITOR
using System;
using Game.Tools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Put this in an Editor assembly/folder
[CustomPropertyDrawer(typeof(CreateSubAssetAttribute))]
public sealed class CreateSubAssetDrawer : PropertyDrawer
{
    private const float BtnW = 26f;
    private const float Gap = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Only makes sense for object references
        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        var parent = property.serializedObject.targetObject as ScriptableObject;
        var attr = (CreateSubAssetAttribute)attribute;

        // No parent SO inspected, no sub-asset parenting possible
        // (You can still show the field normally)
        var fieldRect = position;
        var btnRect = position;

        fieldRect.width -= (BtnW + Gap);
        btnRect.x = fieldRect.xMax + Gap;
        btnRect.width = BtnW;

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.ObjectField(fieldRect, property, label);

        using (new EditorGUI.DisabledScope(parent == null))
        {
            if (GUI.Button(btnRect, "+", EditorStyles.miniButton))
            {
                var baseType = attr.baseType ?? GetFieldReferenceType();
                OpenTypePicker(baseType, (selectedType) =>
                {
                    if (selectedType == null) return;
                    if (!baseType.IsAssignableFrom(selectedType)) return;
                    if (!attr.allowAbstract && (selectedType.IsAbstract || selectedType.IsGenericTypeDefinition)) return;

                    CreateAddAssign(selectedType, parent, property, attr.defaultName);
                });
            }
        }

        EditorGUI.EndProperty();
    }

    private Type GetFieldReferenceType()
    {
        // Field type is something like MySO : ScriptableObject
        var t = fieldInfo.FieldType;
        return t;
    }

    private static void OpenTypePicker(Type baseType, Action<Type> onSelected)
    {
        // Use your existing window if you have it.
        // This matches what you already use in SerializableSOCollection<T>.
        // If your method signature differs, adjust this one line.
        SearchWindowProvider.OpenSearchTypeWindow(
            baseType,
            onSelected
        );

        // If you only have the generic version OpenSearchTypeWindow<T>(), then you need
        // either a non-generic overload (recommended) or a small wrapper in your provider.
    }

    private static void CreateAddAssign(Type concreteType, ScriptableObject parent, SerializedProperty property, string defaultName)
    {
        var child = ScriptableObject.CreateInstance(concreteType);
        child.name = string.IsNullOrWhiteSpace(defaultName) ? concreteType.Name : defaultName;

        // Parent it into the inspected SO asset
        AssetDatabase.AddObjectToAsset(child, parent);

        // Assign reference
        property.serializedObject.Update();
        property.objectReferenceValue = child;
        property.serializedObject.ApplyModifiedProperties();

        // Persist
        EditorUtility.SetDirty(parent);
        EditorUtility.SetDirty(child);
        AssetDatabase.SaveAssets();

        // Helps the Project view update + ensures the sub-asset shows up immediately
        var parentPath = AssetDatabase.GetAssetPath(parent);
        if (!string.IsNullOrEmpty(parentPath))
            AssetDatabase.ImportAsset(parentPath);
    }
}
#endif