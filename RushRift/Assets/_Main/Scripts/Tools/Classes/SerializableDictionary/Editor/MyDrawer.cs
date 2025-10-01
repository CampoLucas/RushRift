using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyTools.Global.Editor
{
    public class MyDrawer : PropertyDrawer
    {
        #region Public Properties

        public float LineHeight { get; private set; }
        public float FullWidth { get; private set; }
        public int FieldCount { get; private set; }
        public SerializedProperty Property { get; private set;}
        public object PropertyObject { get; private set; }

        #endregion

        #region Catched Values

        private SerializedProperty _prevProperty;
        private SerializedObject _prevSerializedObject;
        private object _prevPropertyObject;

        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIBegin(position, property, label);
            GUIStart(position, property, label);
            GUIDraw(position, property, label);
            GUIEnd(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetLineHeight() * FieldCount + MarginTop() + MarginBottom();
        }


        #region Methods

        /// <summary>
        /// returns the line height with a spacing
        /// </summary>
        /// <returns></returns>
        protected virtual float GetLineHeight() => LineHeight + LineSpacing();
        protected virtual float GetLineHeight(Rect position) => position.height + LineSpacing();
        /// <summary>
        /// Returns the full width of the property drawer applying margin
        /// </summary>
        protected virtual float GetPropertyWidth() => FullWidth - MarginRight() - MarginLeft();
        protected virtual float GetPropertyWidth(Rect position) => position.width + LineSpacing();
        protected virtual float LineSpacing() => 0;
        protected virtual float Margin() => 0;
        protected virtual float MarginTop() => 0 + Margin();
        protected virtual float MarginBottom() => 0 + Margin();
        protected virtual float MarginRight() => 0 + Margin();
        protected virtual float MarginLeft() => 0 + Margin();

        #endregion
        

        #region OnGUIMethods

        protected virtual void OnGUIBegin(Rect position, SerializedProperty property, GUIContent label) { }
        protected virtual void OnGUIStart(Rect position, SerializedProperty property, GUIContent label) { }
        protected virtual void OnGUIDraw(Rect position, SerializedProperty property, GUIContent label) { }
        protected virtual void OnGUIEnd(Rect position, SerializedProperty property, GUIContent label) { }

        #endregion

        #region DrawMethods

        protected Rect GetRect(Rect position)
        {
            var yPos = position.y + FieldCount * GetLineHeight();
            FieldCount++;
            return new Rect(position.x + MarginRight(), yPos, GetPropertyWidth(position), LineHeight);
        }

        protected void DrawProperty(Rect position, SerializedProperty property, GUIContent label, bool separateLabel = false)
        {
            if (separateLabel)
            {
                if (label != GUIContent.none)
                    EditorGUI.LabelField(GetRect(position), label);
                EditorGUI.PropertyField(GetRect(position), property, GUIContent.none);
            }
            else
                EditorGUI.PropertyField(GetRect(position), property, label);
        }

        protected void DrawProperty(Rect position, SerializedProperty[] properties, GUIContent label, float spacing = 0)
        {
            var propertiesCount = properties.Length;
            var pos = GetRect(position);
            var width = (GetPropertyWidth(pos)- spacing) / propertiesCount ;
            
            if (label != GUIContent.none)
                EditorGUI.LabelField(GetRect(position), label);
                
            for (var i = 0; i < propertiesCount; i++)
            {
                var xPos = pos.x + i * (width + spacing);
                var rect = new Rect(xPos, pos.y, width, pos.height);
                EditorGUI.PropertyField(rect, properties[i], GUIContent.none);
            }
        }

        protected void DrawObject(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.ObjectField(GetRect(position), property, label);
        }


        #endregion

        private void GUIBegin(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (_prevPropertyObject != PropertyObject)
            {
                PropertyObject = EditorUtils.GetTargetObjectOfProperty(property);
                _prevPropertyObject = PropertyObject;
            }

            if (_prevProperty != property)
            {
                Property = property;
                _prevProperty = Property;
            }
            
            // var editor = UnityEditor.Editor.CreateEditor(property.objectReferenceValue);
            // SerializedObject = editor.serializedObject;
            OnGUIBegin(position, property, label);
        }

        private void GUIStart(Rect position, SerializedProperty property, GUIContent label)
        {
            LineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            FullWidth = position.width;
            FieldCount = 0;

            // if (!IsPropertyEmpty())
            // {
            //     var editor = UnityEditor.Editor.CreateEditor(property.objectReferenceValue);
            //     if (editor)
            //         SerializedObject = editor.serializedObject;
            // }
            
            OnGUIStart(position, property, label);
        }

        private void GUIDraw(Rect position, SerializedProperty property, GUIContent label)
        {
            OnGUIDraw(position, property, label);
        }

        private void GUIEnd(Rect position, SerializedProperty property, GUIContent label)
        {
            OnGUIEnd(position, property, label);
            EditorGUI.EndProperty();
        }
    }
}