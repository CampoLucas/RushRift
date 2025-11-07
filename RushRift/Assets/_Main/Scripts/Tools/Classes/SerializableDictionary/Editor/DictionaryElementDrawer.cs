using UnityEditor;
using UnityEngine;

namespace MyTools.Global.Editor
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>.DictionaryElement))]
    public class DictionaryElementDrawer : MyDrawer
    {
        #region Serialized Properties

        private SerializedProperty _key;
        private SerializedProperty _value;

        #endregion
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
#if false
            var baseHeight  = base.GetPropertyHeight(property, label);

            var key = property.FindPropertyRelative("key");
            var value = property.FindPropertyRelative("value");

            property.isExpanded = key.isExpanded || value.isExpanded;
            
            if (property.isExpanded)
            {
                var keyHeight = EditorGUI.GetPropertyHeight(key, true);;
                var valueHeight = EditorGUI.GetPropertyHeight(value, true);

                var expandedHeight = 0f;

                if (keyHeight > valueHeight)
                {
                    expandedHeight = keyHeight;
                }
                else
                {
                    expandedHeight = valueHeight;
                }

                if (expandedHeight > baseHeight)
                    baseHeight = expandedHeight;
            }
            
            return baseHeight ;
#else
            // Use the automatic line count system
            base.GetPropertyHeight(property, label); // sets LineHeight etc.
            var height = MarginTop();

            if (property.isExpanded)
            {
                var keyHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("key"), true);
                var valueHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("value"), true);
                height += keyHeight + valueHeight + LineSpacing() * 3;
            }
            else
            {
                height += LineHeight + LineSpacing(); // single line
            }

            height += MarginBottom();
            return height;
#endif
        }

        protected override void OnGUIBegin(Rect position, SerializedProperty property, GUIContent label)
        {
            _key = property.FindPropertyRelative("key");
            _value = property.FindPropertyRelative("value");
            property.isExpanded = _key.isExpanded || _value.isExpanded;
        }
        
        

        protected override void OnGUIDraw(Rect position, SerializedProperty property, GUIContent label)
        {
#if false
            //DrawProperty(GetRect(position));
            DrawProperty(position, new [] { _key, _value}, GUIContent.none, 22);
#else
            if (!property.isExpanded)
            {
                // Compact mode: side by side
                DrawProperty(position, new[] { _key, _value }, GUIContent.none, 20);
            }
            else
            {
                // Expanded mode: stacked vertically
                EditorGUI.indentLevel++;
                DrawProperty(position, _key, GUIContent.none);
                DrawProperty(position, _value, GUIContent.none);
                EditorGUI.indentLevel--;
            }
#endif
            
        }

        protected override void OnGUIEnd(Rect position, SerializedProperty property, GUIContent label)
        {
            
        }
        
    }
}