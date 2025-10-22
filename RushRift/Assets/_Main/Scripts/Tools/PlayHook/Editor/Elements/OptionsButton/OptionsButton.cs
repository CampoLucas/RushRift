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
        private const string Name = "options-button";
        private const string UssClassName = "options-button";
        private const string ToolbarVariantClass = UssClassName + "--toolbar";
        private const string WindowVariantClass = UssClassName + "--window";
        
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

        public OptionsButton() : this("Button")
        {
        }

        public OptionsButton(string text, bool isToolbar = false)
        {
            Setup(text, isToolbar);
        }

        private void Setup(string text, bool isToolbar = false)
        {
            AddToClassList(UssClassName);
            if (isToolbar)
                AddToClassList(ToolbarVariantClass);
            else
                AddToClassList(WindowVariantClass);

            // Load and attach stylesheet
            var styleSheet = Resources.Load<StyleSheet>("OptionsStyle");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            else
                Debug.LogWarning("Missing stylesheet: OptionsButton");
            
            // Setup
            _isToolbar = isToolbar;
            
            style.display = DisplayStyle.Flex;
            name = $"{Name}-container";

            _rootElement = GetButton(isToolbar, OpenMenu);
            
            _rootElement.name = Name;
            _rootElement.text = text;
            

            // _rootElement.style.display = DisplayStyle.Flex;
            // _rootElement.style.flexGrow = 1;
            // _rootElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            Add(_rootElement);
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