
using Tools.Scripts.PropertyAttributes;
using UnityEditor;
using UnityEngine;

namespace Tools.Scripts.Classes.Editor
{
    [CustomPropertyDrawer(typeof(Vector3Curve))]
    public class Vector3CurveDrawer : CustomDrawer
    {
        #region ConstValues

        private const string X = "x";
        private const string Y = "y";
        private const string Z = "z";
        private const string SPEED_MODIFIER = "speedModifier";
        private const string SCALE_MODIFIER = "scaleModifier";
        private const string AXIS_MULTIPLIER = "axisMultiplier";

        #endregion

        protected override void OnInitialize(Rect position, SerializedProperty property, GUIContent label, ViewData data)
        {
            base.OnInitialize(position, property, label, data);
            data.AddProperty(X, property);
            data.AddProperty(Y, property);
            data.AddProperty(Z, property);
            data.AddProperty(SPEED_MODIFIER, property);
            data.AddProperty(SCALE_MODIFIER, property);
            data.AddProperty(AXIS_MULTIPLIER, property);

            var pos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            data.lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            data.fullWidth = pos.width - GetMarginLeft() - GetMarginRight();
            data.fieldCount = 0;

        }
        protected override void SetGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);
            
            var foldoutRect = new Rect(position.x, position.y, position.width, GetFoldoutHeight());
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);
            

            var pos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            //Init(pos, property);
            

            EditorGUI.indentLevel = 0;

            var curveRect = GetRect(pos, property);
            DrawCurve(X, curveRect, property,  0, 3, Color.red);
            DrawCurve(Y, curveRect, property,  1, 3, Color.green);
            DrawCurve(Z, curveRect, property,  2, 3, Color.yellow);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawProperty(SPEED_MODIFIER, position, property, false);
                DrawProperty(SCALE_MODIFIER, position, property, false);
                DrawProperty(AXIS_MULTIPLIER, position, property, false);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();

        }
    }
}