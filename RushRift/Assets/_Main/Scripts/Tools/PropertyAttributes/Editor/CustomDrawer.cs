using System.Collections.Generic;
using MyTools.Utils;
using MyTools.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace Tools.Scripts.PropertyAttributes
{
    public class CustomDrawer : PropertyDrawer
    {
        public class ViewData
        {
            public float lineHeight;
            public float fullWidth;
            public int fieldCount;
            private Dictionary<string, SerializedProperty> _properties = new();

            public bool AddProperty(string name, SerializedProperty property)
            {
                if (property == null) return false;
                _properties[name] = property.FindPropertyRelative(name);
                return true;
            }

            public bool RemoveProperty(string name)
            {
                return _properties.Remove(name);
            }

            public bool TryGetProperty(string name, out SerializedProperty property)
            {
                return _properties.TryGetValue(name, out property);
            }
        }
        
        #region ConstValues
        private const float FOLDOUT_HEIGHT = 20f;
        
        // Curve size settings
        private const float LABEL_WIDTH = 12;
        private const float LABEL_SPACING = 5;
        private const float CURVE_SPACING = 2;
        
        // Line size settings
        private const float LINE_SPACING = 3;
        
        // Property size settings
        private const float MARGIN_TOP = 0;
        private const float MARGIN_BOTTOM = 10;
        private const float MARGIN_RIGHT = 0;
        private const float MARGIN_LEFT = 0;
        
        #endregion

        private Dictionary<string, ViewData> _perPropertyData = new();
        
        private void Initialize(Rect position, SerializedProperty property, GUIContent label)
        {
            var path = property.propertyPath;
            if (!_perPropertyData.TryGetValue(path, out var viewData))
            {
                viewData = new ViewData();
                _perPropertyData[path] = viewData;
            }
            
            OnInitialize(position, property, label, viewData);
        }


        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(position, property, label);
            SetGUI(position, property, label);
            //base.OnGUI(position, property, label);
        }

        protected bool TryGetData(SerializedProperty property, out ViewData data)
        {
            if (_perPropertyData == null)
            {
                Debug.LogWarning("WARNING: '_perPropertyData' is null");
                data = null;
                return false;
            }
            return _perPropertyData.TryGetValue(property.propertyPath, out data);
        }

        protected ViewData GetData(SerializedProperty property)
        {
            if (TryGetData(property, out var data))
            {
                return data;
            }
            else
            {
                Debug.LogWarning("WARNING: ViewData not found");
                return null;
            }
        }
        
        protected virtual void OnInitialize(Rect position, SerializedProperty property, GUIContent label, ViewData data) { }
        protected virtual void SetGUI(Rect position, SerializedProperty property, GUIContent label) { }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!TryGetData(property, out var data)) return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            return data.lineHeight * data.fieldCount + GetLineSpacing() * (data.fieldCount - 1);
        }

        protected Rect GetRect(Rect position, SerializedProperty property)
        {
            var data = GetData(property);
            
            var yPos = position.y + data.fieldCount * (data.lineHeight + GetLineSpacing());
            data.fieldCount++;
            return new Rect(position.x + GetMarginRight(), yPos + GetMarginTop(), GetFullWidth(position), data.lineHeight);
        }

        public void DrawVerticalCells(Rect position, SerializedProperty property, string label, string[] names)
        {
            var data = GetData(property);
            var pos = GetRect(position, property);

            var pp = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(label));
            var propertyWidth = pp.width / names.Length;
            var labelWidth = GetLabelWidth() + GetLineSpacing();
            var capsuleWidth = propertyWidth - labelWidth - CURVE_SPACING;

            var index = 0;
            
            for (var i = 0; i < names.Length; i++)
            {
                if (!data.TryGetProperty(names[i], out var p)) continue;
                
                var capsuleLabelPosition = pp.x + index * (propertyWidth + CURVE_SPACING);
                var curvePosition = capsuleLabelPosition + labelWidth - CURVE_SPACING;
                
                var labelRect = new Rect(capsuleLabelPosition, pp.y, pp.width, pp.height);
                var capsuleRect = new Rect(curvePosition, pp.y + GetLineSpacing()/2, capsuleWidth, pp.height * 0.85f);
                EditorGUI.LabelField(labelRect, p.name.ToUpper());
                //EditorGUI.PropertyField(curveRect, property, GUIContent.none);
            
                //var curve = (AnimationCurve)EditorUtils.GetTargetObjectOfProperty(p);
                
                EditorGUI.PropertyField(capsuleRect, p, GUIContent.none);
                index++;
            }
        }
        
        public void DrawCurve(string name, Rect position, SerializedProperty property, float row, float amount, Color color)
        {
            var data = GetData(property);
            if (!data.TryGetProperty(name, out var p)) return;
            
            var propertyWidth = data.fullWidth / amount;
            var labelWidth = GetLabelWidth() + GetLineSpacing();
            var curveWidth = propertyWidth - labelWidth - CURVE_SPACING;
            
            var labelPosition = position.x + row * (propertyWidth + CURVE_SPACING);
            var curvePosition = labelPosition + labelWidth - CURVE_SPACING;

            var labelRect = new Rect(labelPosition, position.y, labelWidth, position.height);
            var curveRect = new Rect(curvePosition, position.y + GetLineSpacing()/2, curveWidth, position.height * 0.85f);
            
            EditorGUI.LabelField(labelRect, p.name.ToUpper());
            //EditorGUI.PropertyField(curveRect, property, GUIContent.none);
            
            var curve = (AnimationCurve)EditorUtils.GetTargetObjectOfProperty(p);
            EditorGUI.CurveField(curveRect, curve, color, new Rect());
        }

        protected void DrawProperty(string name, Rect position, SerializedProperty property, bool label)
        {
            var data = GetData(property);
            if (!data.TryGetProperty(name, out var p)) return;
            var propertyName = StringFormatter.ConvertStringFormat(p.name);

            if (label)
            {
                EditorGUI.LabelField(GetRect(position, property), propertyName);
                EditorGUI.PropertyField(GetRect(position, property), p, GUIContent.none);
            }
            else
            {
                EditorGUI.PropertyField(GetRect(position, property), p, new GUIContent(propertyName));
            }
        }
        
        protected float GetFullWidth(Rect position) => position.width - GetMarginLeft() - GetMarginRight();

        protected virtual float GetFoldoutHeight() => FOLDOUT_HEIGHT;
        protected virtual float GetLabelWidth() => LABEL_WIDTH;
        protected virtual float GetLabelSpacing() => LABEL_SPACING;
        protected virtual float GetLineSpacing() => LINE_SPACING;
        protected virtual float GetMarginTop() => MARGIN_TOP;
        protected virtual float GetMarginBottom() => MARGIN_BOTTOM;
        protected virtual float GetMarginRight() => MARGIN_RIGHT;
        protected virtual float GetMarginLeft() => MARGIN_LEFT;
    }
}