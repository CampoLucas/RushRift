using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeAsset.EditorUI;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ContentZoomer = UnityEditor.Experimental.GraphView.ContentZoomer;
using Edge = UnityEditor.Experimental.GraphView.Edge;

public class BehaviourTreeView : GraphView
{
    public new class UxmlFactory : UxmlFactory<BehaviourTreeView, UxmlTraits>
    {
    }

    public Action<NodeView> OnNodeSelected;

    private BehaviourTreeData _tree;

    public BehaviourTreeView()
    {
        SetGridBackground();
        SetManipulators();
        SetStyleSheet();
    }

    private void SetGridBackground()
    {
        Insert(0, new GridBackground());
    }

    private void SetManipulators()
    {
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // var mMap = new MiniMap();
        // mMap.SetPosition(new Rect(mMap.GetPosition().x, mMap.GetPosition().y, 200, 150));
        //
        // Add(mMap);
    }

    private void SetStyleSheet()
    {
        var styleSheet = Resources.Load("BehaviourTreeStyle") as StyleSheet;
        styleSheets.Add(styleSheet);
    }

    internal void PopulateView(BehaviourTreeData tree)
    {
        _tree = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        if (tree.Root == null)
        {
            tree.SetRootNode(tree.CreateNode(typeof(RootData)));
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }
        
        CreateNodes(_tree);
        CreateEdges(_tree);
    }

    #region Events

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        var elementsToRemove = graphViewChange.elementsToRemove;
        if (elementsToRemove != null)
        {
            for (var i = 0; i < elementsToRemove.Count; i++)
            {
                var element = elementsToRemove[i];
                if (element is NodeView nodeView)
                {
                    OnDeleteNode(nodeView);
                }

                if (element is Edge edge)
                {
                    OnEdgeToDelete(edge);
                }
            }
        }

        var edgesToCreate = graphViewChange.edgesToCreate;
        if (edgesToCreate != null)
        {
            for (var i = 0; i < edgesToCreate.Count; i++)
            {
                var edge = edgesToCreate[i];

                OnEdgeToCreate(edge, ref edgesToCreate);
            }
        }
        
        return graphViewChange;
    }

    private void OnDeleteNode(NodeView nodeView)
    {
        _tree.DeleteNode(nodeView.NodeData);
    }

    private void OnEdgeToCreate(Edge edge, ref List<Edge> edgesToCreate)
    {
        var parentView = (NodeView)edge.output.node;
        var childView = (NodeView)edge.input.node;
                
        if (parentView == null || childView == null) return;

        if (!_tree.AddChild(parentView.NodeData, childView.NodeData))
        {
            edgesToCreate.Remove(edge);
        }
    }

    private void OnEdgeToDelete(Edge edge)
    {
        var parentView = (NodeView)edge.output.node;
        var childView = (NodeView)edge.input.node;
                
        if (parentView == null || childView == null) return;

        _tree.RemoveChild(parentView.NodeData, childView.NodeData);
    }

    #endregion

    #region Nodes

    private void CreateNodes(BehaviourTreeData tree)
    {
        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var node = tree.Nodes[i];
            CreateNodeView(node);
        }
    }

    void CreateNode(System.Type type, Vector2 pos)
    {
        //if (_tree == null) Debug.LogError("Tree is null");
        var node = _tree.CreateNode(type);
        node.Position = pos;
        CreateNodeView(node);
    }

    private void CreateNodeView(NodeData nodeData)
    {
        var nodeView = new NodeView(nodeData);
        nodeView.OnNodeSelected = OnNodeSelected;
        AddElement(nodeView);
    }

    private NodeView FindNodeView(NodeData nodeData)
    {
        return GetNodeByGuid(nodeData.guid) as NodeView;
    }

    #endregion

    #region Edges

    private void CreateEdges(BehaviourTreeData tree)
    {
        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var node = tree.Nodes[i];
            var children = tree.GetChildren(node);

            if (children == null || children.Count == 0) continue;
            for (var j = 0; j < children.Count; j++)
            {
                var child = children[j];

                var parentView = FindNodeView(node);
                var childView = FindNodeView(child);

                
                var edge = parentView.Out.ConnectTo(childView.In);
                AddElement(edge);
            }
        }
    }

    #endregion

    #region Ports

    /// <summary>
    /// Checks if ports can conect.
    /// </summary>
    /// <param name="startPort"></param>
    /// <param name="nodeAdapter"></param>
    /// <returns></returns>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var p = new List<Port>();
        var portsList = ports.ToList();

        var startNode = startPort.node as NodeView;
        
        for (var i = 0; i < portsList.Count(); i++)
        {
            var endPort = portsList[i];

            var endNode = endPort.node as NodeView;

            if (startNode == null || endNode == null)
            {
                if (endPort.direction != startPort.direction && endPort.node != startPort.node)
                {
                    p.Add(endPort);
                }
            }
            else
            {
                if (endPort.direction != startPort.direction && endPort.node != startPort.node && 
                    !endNode.NodeData.ContainsChildInChildren(startNode.NodeData))
                {
                    p.Add(endPort);
                }
            }

            
        }

        return p;
    }

    #endregion
    
    #region Search Bar

    // /// <summary>
    // /// This is for the search bar ToDo: Change it to a proper search bar...
    // /// </summary>
    // /// <param name="evt"></param>
    // public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    // {
    //     //base.BuildContextualMenu(evt);
    //
    //     var types = TypeCache.GetTypesDerivedFrom<INode>();
    //
    //     for (var i = 0; i < types.Count; i++)
    //     {
    //         var type = types[i];
    //         if (!type.IsAbstract && !type.IsSealed)
    //         {
    //             var baseType = type.BaseType;
    //             evt.menu.AppendAction($"{baseType.Name}/ {type.Name}", 
    //                 (a) => CreateNode(type, Vector2.zero));
    //         }
    //     }
    // }
    
    // /// <summary>
    /// This is for the search bar ToDo: Change it to a proper search bar...
    /// </summary>
    /// <param name="evt"></param>
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var types = TypeCache.GetTypesDerivedFrom<INodeData>();
    
        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            if (!type.IsAbstract && !type.IsSealed)
            {
                var baseType = type.BaseType;
                evt.menu.AppendAction($"{baseType.Name}/ {type.Name}", (a) =>
                {
                    CreateNode(type, contentViewContainer.WorldToLocal(a.eventInfo.mousePosition));
                });
            }
        }
    }
    
    
    

    #endregion
    
    
}
