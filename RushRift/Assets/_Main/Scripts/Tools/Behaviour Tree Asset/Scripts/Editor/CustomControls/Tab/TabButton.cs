using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements
{
    public sealed class TabButton : Button
    {
	    public static readonly string ButtonClass = "tab-button";
	    public static readonly string ActiveClass = "active";
	    public static readonly string NotActiveClass = "not-active";

	    public TabButton(string tabName)
        {
            name = "tab-button";
            text = tabName;
            
            SetUSSClasses();
            SetStyle();
        }

        public void SetUSSClasses()
        {
	        AddToClassList(ButtonClass);
        }

        public void SetStyle()
        {
#if false
            style.backgroundColor = new StyleColor(Color.red);
#else
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
#endif
        }

        public void Select()
        {
	        RemoveFromClassList(NotActiveClass);
	        AddToClassList(ActiveClass);
	        //ToggleInClassList("show");
        }

        public void Unselect()
        {
	        RemoveFromClassList(ActiveClass);
	        AddToClassList(NotActiveClass);
	        //ToggleInClassList("hide");
        }
    }
}