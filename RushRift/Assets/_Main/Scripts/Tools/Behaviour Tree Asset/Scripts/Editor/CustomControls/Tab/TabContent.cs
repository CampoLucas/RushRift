using System;
using System.Collections.Generic;
using BehaviourTreeAsset.EditorUI.VisualElements.Interfeces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements
{
    public class TabContent : VisualElement, ITabContent
    {
        public static readonly string ContentClass = "tab-content";
        public static readonly string ContentName = "tab-content";
        public static readonly string ActiveClass = "tab-active";
        public static readonly string ElementClass = "tab-element";
        
        public VisualElement CurrentElement { get; private set; }
        
        private List<VisualElement> _contents = new();
        private Dictionary<Type, List<VisualElement>> _typeDictionary = new();
        
        public TabContent()
        {
            name = ContentName;
            AddToClassList(ContentClass);
            
            SetStyle();
        }

        public void AddTabElement<T>(T element) 
            where T : VisualElement
        {
            _contents.Add(element);
            element.AddToClassList(ElementClass);
            Add(element);
            if (!_typeDictionary.TryGetValue(typeof(T), out var eList))
            {
                eList = new List<VisualElement>();
                _typeDictionary[typeof(T)] = eList;
            }
            
            eList.Add(element);
        }

        public void RemoveTabElement<T>(T element) 
            where T : VisualElement
        {
            _contents.Remove(element);
            Remove(element);
            if (_typeDictionary.TryGetValue(typeof(T), out var eList))
            {
                eList.Remove(element);
            }
        }

        public void SetCurrentElement(VisualElement element)
        {
            if (CurrentElement == element) return;
            if (CurrentElement != null)
                CurrentElement.RemoveFromClassList(ActiveClass);
            CurrentElement = element;
            CurrentElement.AddToClassList(ActiveClass);
            
            // if (CurrentElement == element) return;
            // if (CurrentElement != null)
            //     Remove(CurrentElement);
            // CurrentElement = element;
            // Add(CurrentElement);
        }

        public T GetTabElement<T>() 
            where T : VisualElement
        {
            return (T)_typeDictionary[typeof(T)][0];
        }

        public bool TryGetTabElement<T>(out T element) 
            where T : VisualElement
        {
            element = GetTabElement<T>();
            return element != null;
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