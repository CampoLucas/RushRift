using System.Collections.Generic;
using BehaviourTreeAsset.EditorUI.VisualElements.Interfeces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements
{
    public class TabContainer : VisualElement, ITabContainer
    {
        public static readonly string ContainerName = "tab-container";
        public static readonly string ContainerClass = "tab-container";
        
        private List<TabButton> _tabButtons = new();

        public TabContainer()
        {
            name = ContainerName;
            AddToClassList(ContainerClass);
            style.flexDirection = FlexDirection.Row;
            
            SetStyle();
        }

        public void AddTab(TabButton button)
        {
            _tabButtons.Add(button);
            Add(button);
        }

        public void RemoveTab(TabButton button)
        {
            if (!_tabButtons.Contains(button)) return;
            Remove(button);
            _tabButtons.Remove(button);
        }

        public void RemoveTab(int index)
        {
            if (_tabButtons.Count - 1 < index || index < 0) return;
            Remove(_tabButtons[index]);
            _tabButtons.RemoveAt(index);
        }

        public void Select(int index)
        {
            if (_tabButtons.Count - 1 < index || index < 0) return;
            _tabButtons[index].Select();;
        }

        public void SelectAll()
        {
            for (var i = 0; i < _tabButtons.Count; i++)
            {
                Select(i);
            }
        }

        public void Unselect(int index)
        {
            if (_tabButtons.Count - 1 < index || index < 0) return;
            _tabButtons[index].Unselect();;
        }

        public void UnselectAll()
        {
            for (var i = 0; i < _tabButtons.Count; i++)
            {
                Unselect(i);
            }
        }

        public void SetCurrentTab(int index)
        {
            if (_tabButtons.Count - 1 < index || index < 0) return;
            UnselectAll();
            Select(index);
        }

        private void SetStyle()
        {
            var styleSheet = Resources.Load("Styles/GlobalStyle") as StyleSheet;
            StyleSheet colorStyleSheet;
            if (EditorGUIUtility.isProSkin)
            {
                colorStyleSheet = Resources.Load("Styles/DarkStyle") as StyleSheet;
            }
            else
            {
                colorStyleSheet = Resources.Load("Styles/LightStyle") as StyleSheet;
            }
		
            styleSheets.Add(styleSheet);
            styleSheets.Add(colorStyleSheet);
        }
    }
}