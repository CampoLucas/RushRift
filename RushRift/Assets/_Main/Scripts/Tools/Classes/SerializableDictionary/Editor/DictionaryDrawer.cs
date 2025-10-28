using System;
using UnityEditor;
using UnityEngine;

namespace MyTools.Global.Editor
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class DictionaryDrawer : MyDrawer
    {
        #region Serialized Properties

        private SerializedProperty _data;

        #endregion
        
        
        protected override void OnGUIBegin(Rect position, SerializedProperty property, GUIContent label)
        {
            _data = property.FindPropertyRelative("data");
        }

        protected override void OnGUIDraw(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawProperty(position, _data, label);
            //DrawDictionary(position, _data, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = base.GetPropertyHeight(property, label);

            try
            {
                _data = property.FindPropertyRelative("data");
                
                if (_data != null && _data.isExpanded)
                {
                    var h = EditorGUI.GetPropertyHeight(_data);
                    if (h > height)
                    {
                        height = h;
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Debug.Log($"ERROR: {e}");
            }
            
            
            return height;
        }
        
        protected void DrawDictionary(Rect position, SerializedProperty property, GUIContent label)
        {
            // for (var i = 0; i < property.arraySize; i++)
            // {
            //     var element = property.GetArrayElementAtIndex(i);
            //     DrawProperty(position, element, GUIContent.none);
            // }

            var foldoutRect = GetRect(position);
            Property.isExpanded = EditorGUI.Foldout(foldoutRect, Property.isExpanded, label);
            if (Property.isExpanded)
            {
                EditorGUI.indentLevel++;
            }

        }
        
    }
}