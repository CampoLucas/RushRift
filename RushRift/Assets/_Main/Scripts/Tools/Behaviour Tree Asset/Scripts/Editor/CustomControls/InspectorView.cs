using BehaviourTreeAsset.EditorUI;
using Codice.CM.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class InspectorView : InspectorElement
{
    public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits>
    {
    }
}

public class InspectorElement : VisualElement
{
    private Editor _editor;
    
    public InspectorElement()
    {
        name = "inspector-element";
        
        SetStyle();
    }
    
    public void ClearSelection()
    {
        Clear();
        UnityEngine.Object.DestroyImmediate(_editor);
    }

    public void UpdateSelection(NodeView nodeView)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(nodeView.NodeData);
        //var container = new IMGUIContainer(() => { _editor.OnInspectorGUI(); });
        if (_editor == null) return;
        var container = new IMGUIContainer(() => { _editor.OnInspectorGUI(); });
        Add(container);
    }
    
    private void SetStyle()
    {
        var styleSheet = Resources.Load("Styles/GlobalStyle") as StyleSheet;
        StyleSheet colorStyleSheet;
        if (EditorGUIUtility.isProSkin)
        {
            colorStyleSheet = Resources.Load("Styles/LightStyle") as StyleSheet;
        }
        else
        {
            colorStyleSheet = Resources.Load("Styles/DarkStyle") as StyleSheet;
        }
		
        styleSheets.Add(styleSheet);
        styleSheets.Add(colorStyleSheet);
    }
}