using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Action = System.Action;

namespace Tools.PlayHook.Elements.Menu
{
    public class Menu : IDisposable
    {
        private readonly List<MenuEntry> _entries = new();
        
        public GenericMenu GetMenu(VisualElement parent)
        {
            var menu = new GenericMenu();

            foreach (var entry in _entries)
            {
                entry.Setup(ref menu);
            }

            var world = parent.worldBound;
            menu.DropDown(new Rect(world.xMin, world.yMax, 0, 0));

            return menu;
        }

        public void SetMenuEntries(IEnumerable<MenuEntry> entries)
        {
            _entries.Clear();
            _entries.AddRange(entries);
        }

        public void Dispose()
        {
            foreach (var entry in _entries)
            {
                entry.Dispose();
            }
            
            _entries.Clear();
        }
    }

    public abstract class MenuEntry : IDisposable
    {
        public abstract void Setup(ref GenericMenu menu, string group = null);
        public abstract void Dispose();
    }
    
    public class MenuItem : MenuEntry
    {
        private readonly string _label;
        private GenericMenu.MenuFunction _onExecute;
        private Func<bool> _isOn;
        private bool _on;
        private Func<bool> _disabled;

        public MenuItem(string label, GenericMenu.MenuFunction onExecute, bool isOn, Func<bool> disabled) : this(label,
            onExecute, null, disabled)
        {
            _on = isOn;
        }
        
        public MenuItem(string label, GenericMenu.MenuFunction onExecute, Func<bool> isOn, Func<bool> disabled)
        {
            _label = label;
            _onExecute = onExecute;
            _isOn = isOn;
            _disabled = disabled;
        }
        
        public override void Setup(ref GenericMenu menu, string group = null)
        {
            var label = string.IsNullOrEmpty(_label) ? "Empty" : _label;

            if (!string.IsNullOrEmpty(group))
            {
                label = $"{group}/{label}";
            }
            
            var content = new GUIContent(label);
            
            if (_disabled != null && _disabled())
            {
                menu.AddDisabledItem(content);
                return;
            }
            
            menu.AddItem(content, _isOn?.Invoke() ?? _on, _onExecute);
        }

        public override void Dispose()
        {
            _onExecute = null;
            _isOn = null;
            _disabled = null;
        }
    }

    public class MenuGroup : MenuEntry
    {
        private readonly string _groupName;
        private List<MenuEntry> _entries = new();

        public MenuGroup(string name)
        {
            _groupName = name;
        }

        public MenuGroup(string name, IEnumerable<MenuEntry> entries) : this(name)
        {
            _entries.AddRange(entries);
        }

        public void Add(MenuEntry entry)
        {
            _entries.Add(entry);
        }
        
        public override void Setup(ref GenericMenu menu, string group = null)
        {
            var label = string.IsNullOrEmpty(_groupName) ? "Empty" : _groupName;

            if (!string.IsNullOrEmpty(group))
            {
                label = $"{group}/{label}";
            }

            foreach (var entry in _entries)
            {
                entry.Setup(ref menu, label);
            }
        }

        public override void Dispose()
        {
            foreach (var entry in _entries)
            {
                entry.Dispose();
            }
            
            _entries.Clear();
            _entries = null;
        }
    }

    public class MenuSeparator : MenuEntry
    {
        public override void Setup(ref GenericMenu menu, string group = null)
        {
            var path = string.IsNullOrEmpty(group) ? "" : group;
            
            menu.AddSeparator(path);
        }

        public override void Dispose()
        {
            
        }
    }
}