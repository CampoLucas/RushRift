using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomPropertyDrawer(typeof(SerializableSO), true)]
    [CustomPropertyDrawer(typeof(SerializableSOAttribute), true)]

    public class PropertySODrawer : PropertyDrawer
    {
        private UnityEditor.Editor _editor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Object.DestroyImmediate(_editor);

            var hasObjectReference = property.objectReferenceValue != null;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(position, property, label);

            if (property.isExpanded)
            {
                if (hasObjectReference)
                {
                    _editor = UnityEditor.Editor.CreateEditor(property.objectReferenceValue);
                    if (_editor != null)
                    {
                        //EditorGUI.indentLevel++;
                        _editor.OnInspectorGUI();
                        EditorGUILayout.Space(2);
                        EditorGUILayout.HelpBox(new GUIContent(
                            "WARNING: Modifications to any properties will be applied to the ScriptableObject's original asset."));
                        //EditorGUI.indentLevel--;
                        EditorGUILayout.Space(15);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("The object reference is null.");
                }
            }



            EditorGUI.indentLevel--;
        }
    }
}
