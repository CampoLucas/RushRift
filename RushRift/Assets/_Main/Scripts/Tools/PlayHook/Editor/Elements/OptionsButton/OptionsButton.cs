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
    public class OptionsButton : VisualElement
    {
        public static readonly string UxmlPath = "OptionsUxml";
        public static readonly string UssPath = "OptionsUss";

        #region Uss Classes
        
        private const string ButtonClass = "options-button";
        private const string ContainerClass = "options-container";
        private string ToolbarVariant(string className) => className + "--toolbar";
        private string WindowVariant(string className) => className + "--window";
        #endregion
        private const string Name = "options-button";
        
        private Button _rootElement;
        private bool _isToolbar;

        private Action<OptionsButton> _onOpenMenu;
        private Func<List<MenuEntry>> _getEntries;
        private Menu.Menu _menu = new();
        
        public new class UxmlFactory : UxmlFactory<OptionsButton, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlBoolAttributeDescription m_IsToolbar = new() { name = "is-toolbar", defaultValue = false };
            private UxmlStringAttributeDescription m_Text = new() { name = "text", defaultValue = "Options" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var element = (OptionsButton)ve;
                element.Setup(
                    m_Text.GetValueFromBag(bag, cc),
                    m_IsToolbar.GetValueFromBag(bag, cc)
                );
            }
        }

        public OptionsButton() : this("Empty")
        {
        }

        public OptionsButton(string text, bool isToolbar = false)
        {
            Setup(text, isToolbar);
        }

        public OptionsButton(string text, string[] classes, bool isToolbar) : this(text, isToolbar)
        {
            for (var i = 0; i < classes.Length; i++)
            {
                AddToClassList(classes[i]);
                _rootElement.AddToClassList(classes[i]);
            }
        }
        
        public OptionsButton(string text, string className, bool isToolbar) : this(text, new string[] {className}, isToolbar)
        {
            
        }

        private void Setup(string text, bool isToolbar = false)
        { 
            // Setup
            _isToolbar = isToolbar;
            name = $"{Name}-container";

            _rootElement = GetButton(isToolbar, OpenMenu);
            _rootElement.name = Name;
            _rootElement.text = text;
            
            SetStyle(isToolbar);
            
            Add(_rootElement);
        }

        private void SetStyle(bool isToolbar)
        {
            // Add classes to container
            AddToClassList(ContainerClass);
            if (isToolbar)
                AddToClassList(ToolbarVariant(ContainerClass));
            else
                AddToClassList(WindowVariant(ContainerClass));
            
            // Add classes to button
            _rootElement.RemoveFromClassList("unity-text-element");
            _rootElement.RemoveFromClassList("unity-button");
            
            _rootElement.AddToClassList(ButtonClass);
            if (isToolbar)
                _rootElement.AddToClassList(ToolbarVariant(ButtonClass));
            else
                _rootElement.AddToClassList(WindowVariant(ButtonClass));

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

        public void RegisterCallback(Action<OptionsButton> onOpenMenu, Func<List<MenuEntry>> getEntries)
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
        
        private Button GetButton(bool isToolbar, Action action)
        {
            return isToolbar ? new EditorToolbarButton(action) : new Button(action);
        }
        
        private Button GetButton(bool isToolbar)
        {
            return isToolbar ? new EditorToolbarButton() : new Button();
        }
        
        public void SetText(string text) => _rootElement.text = text;

        
    }
}