using System;
using System.Collections.Generic;
using BehaviourTreeAsset.EditorUI.VisualElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class TabView : VisualElement
{
	public new class UxmlFactory : UxmlFactory<TabView, UxmlTraits>
	{
	}
	
    private int _currentTab;
    private int _tabCount;
    private List<Tab> _tabs = new();
	private TabContainer _container;
	private TabContent _content;

	public TabView()
	{
		name = "tab-view";
		_container = new TabContainer();
		_content = new TabContent();
		
		Add(_container);
		Add(_content);
		
		
		
		_container.SetCurrentTab(_currentTab);
		SetStyle();
	}

	public void SetCurrentTab(VisualElement content, int tabIndex)
	{
		_currentTab = tabIndex;
		_container.SetCurrentTab(_currentTab);
		_content.SetCurrentElement(content);
	}

	public void AddTab<T>(string tabName, T tabContent) where T : VisualElement
	{
		var tab = new Tab(tabName, tabContent, _tabCount);
		tab.Button.clicked += () => { SetCurrentTab(tab.Content, tab.Index); };
		_container.AddTab(tab.Button);
		_content.AddTabElement(tab.Content);

		if (_tabCount == 0)
		{
			_currentTab = _tabCount;
			_content.SetCurrentElement(tab.Content);
		}

		//SetCurrentTab(tab.Content, _currentTab);
		_container.SetCurrentTab(_currentTab);
		_tabCount++;
	}

	public void RemoveTab(string name)
	{
		
	}

	public void SetStyle()
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

	public T GetTabElement<T>() where T : VisualElement
	{
		return _content.GetTabElement<T>();
	}

	public bool TryGetTabElement<T>(out T element) where T : VisualElement
	{
		return _content.TryGetTabElement(out element);
	}

	public class Tab
	{
		public TabButton Button { get; private set; }
		public VisualElement Content { get; private set; }
		public bool Enable { get; private set; }
		public int Index { get; private set; }
		
		public Tab(string name, VisualElement content, int index)
		{
			Button = new TabButton(name);
			Content = content;
			Index = index;
		}

		public void SetEnable(bool value)
		{
			Enable = value;
		}
	}
}