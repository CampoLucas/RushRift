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
            var baseHeight  = base.GetPropertyHeight(property, label);

            var key = property.FindPropertyRelative("key");
            var value = property.FindPropertyRelative("value");

            property.isExpanded = key.isExpanded || value.isExpanded;
            
            if (property.isExpanded)
            {
                // if (_key != null && _key.isExpanded)
                // {
                //     var h = EditorGUI.GetPropertyHeight(_key);
                //
                //     if (h > height)
                //     {
                //         height = h;
                //     }
                // }
                // if (_value != null && _value.isExpanded)
                // {
                //     var h = EditorGUI.GetPropertyHeight(_value);
                //
                //     if (h > height)
                //     {
                //         height = h;
                //     }
                // }

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
        }

        protected override void OnGUIBegin(Rect position, SerializedProperty property, GUIContent label)
        {
            _key = property.FindPropertyRelative("key");
            _value = property.FindPropertyRelative("value");
            property.isExpanded = _key.isExpanded || _value.isExpanded;
        }
        
        

        protected override void OnGUIDraw(Rect position, SerializedProperty property, GUIContent label)
        {
            //DrawProperty(GetRect(position));
            DrawProperty(position, new [] { _key, _value}, GUIContent.none, 22);
            
        }

        protected override void OnGUIEnd(Rect position, SerializedProperty property, GUIContent label)
        {
            
        }
        
    }
}