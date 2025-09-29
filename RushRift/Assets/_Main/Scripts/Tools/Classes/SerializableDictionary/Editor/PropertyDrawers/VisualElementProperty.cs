using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyTools.Global.Editor
{
    public class VisualElementProperty : PropertyDrawer
    {
        public sealed override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Initialize(property, out var root);
            Compose(property, root);
            return root;
        }

        private void Initialize(SerializedProperty property, out VisualElement root)
        {
            root = new VisualElement();
            OnInitialize(property, root);
        }

        private void Compose(SerializedProperty property, VisualElement root)
        {
            OnCompose(property, root);
        }
        
        protected virtual void OnInitialize(SerializedProperty property, VisualElement root) { }
        protected virtual void OnCompose(SerializedProperty property, VisualElement root) { }
    }
}