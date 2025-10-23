using System;
using System.Collections.Generic;
using Tools.PlayHook.Elements.Menu;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using MenuItem = Tools.PlayHook.Elements.Menu.MenuItem;

namespace Tools.PlayHook.Elements
{
    public class VariantDropdownButton : VisualElement
    {
        public static readonly string UxmlPath = "VariantElementUxml";
        public static readonly string UssPath = "VariantElementUss";

        #region Uss Classes
        
        private const string ButtonClass = "variant-element-button";
        private const string ContainerClass = "variant-element-container";
        private string Variant(string className, ElementVariant variant) => $"{className}--{variant.ToString().ToLower()}";
        #endregion
        private const string Name = "options-button";
        
        private Button _rootElement;

        private Action<VariantDropdownButton> _onOpenMenu;
        private Func<List<MenuEntry>> _getEntries;
        private Menu.Menu _menu = new();
        private ElementVariant _variant;
        
        public VariantDropdownButton() : this("Empty")
        {
        }

        public VariantDropdownButton(string text, ElementVariant variant = ElementVariant.Overlay)
        {
            Setup(text, variant);
        }

        public VariantDropdownButton(string text, string[] classes, ElementVariant variant) : this(text, variant)
        {
            for (var i = 0; i < classes.Length; i++)
            {
                AddToClassList(classes[i]);
                _rootElement.AddToClassList(classes[i]);
            }
        }
        
        public VariantDropdownButton(string text, string className, ElementVariant variant) : this(text, new string[] {className}, variant)
        {
            
        }

        private void Setup(string text, ElementVariant variant = ElementVariant.Overlay)
        { 
            // Setup
            name = $"{Name}-container";

            _rootElement = GetButton(variant, OpenMenu);
            _rootElement.name = Name;
            _rootElement.text = text;
            
            SetStyle(variant);
            
            Add(_rootElement);
        }

        private void SetStyle(ElementVariant variant)
        {
            // Add classes to container
            AddToClassList(ContainerClass);
            AddToClassList(Variant(ContainerClass, variant));
            
            // Add classes to button
            _rootElement.RemoveFromClassList("unity-text-element");
            _rootElement.RemoveFromClassList("unity-button");
        
            // _rootElement.AddToClassList(ButtonClass);
            _rootElement.AddToClassList(Variant(ButtonClass, variant));
            

            // Setup Uxml
            var uiFile = AssetDatabase.GetAssetPath(Resources.Load(UxmlPath));
            (EditorGUIUtility.Load(uiFile) as VisualTreeAsset)?.CloneTree(this);
            
            // Load and attach stylesheet
            var styleSheet = Resources.Load<StyleSheet>(UssPath);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            else
                Debug.LogWarning("Missing stylesheet: OptionsButton");
        }

        public void RegisterCallback(Action<VariantDropdownButton> onOpenMenu, Func<List<MenuEntry>> getEntries)
        {
            _onOpenMenu = onOpenMenu;
            _getEntries = getEntries;
        }

        private void OnOpenButton()
        {
            if (_onOpenMenu != null)
            {
                _onOpenMenu(this);
            }
        }

        private List<MenuEntry> GetEntries()
        {
            var entries = new List<MenuEntry>();
            
            if (_getEntries != null)
            {
                entries.AddRange(_getEntries());
            }

            if (entries.Count == 0)
            {
                entries.Add(new MenuItem("No options", null, Disabled, Disabled));
            }

            return entries;
        }

        private bool Disabled() => false;

        private void OpenMenu()
        {
            OnOpenButton();

            _menu.SetMenuEntries(GetEntries());
            _menu.GetMenu(this);
        }
        
        private Button GetButton(ElementVariant variant, Action action)
        {
            return variant == ElementVariant.Overlay ? new EditorToolbarButton(action) : new Button(action);
        }
        
        private Button GetButton(bool isToolbar)
        {
            return isToolbar ? new EditorToolbarButton() : new Button();
        }

        public void SetText(string text)
        {
            _rootElement.text = text;
        }

        
    }
}