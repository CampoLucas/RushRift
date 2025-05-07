using System;
using BehaviourTreeAsset.EditorUI.VisualElements.Interfeces;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Node;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node, IStyleable
    {
        public static readonly string UxmlNodePath = "NodeUI";
        public static readonly string UxmlPortPath = "PortUI";
        public static readonly string ActionClass = "action";
        public static readonly string ConditionalClass = "conditional";
        public static readonly string DecoratorClass = "decorator";
        public static readonly string CompositeClass = "composite";
        public static readonly string RootClass = "root";
        
        public sealed override string title { get => base.title; set => base.title = value; }
        public NodeData NodeData { get; private set; }
        public Port In { get; private set; }
        public Port Out { get; private set; }

        public Action<NodeView> OnNodeSelected;
        
        public NodeView(NodeData nodeData) : base(AssetDatabase.GetAssetPath(Resources.Load(UxmlNodePath)))
        {
            NodeData = nodeData;
            title = nodeData.Name;
            viewDataKey = NodeData.guid;
            
            style.left = nodeData.Position.x;
            style.top = nodeData.Position.y;
            
            CreateInPorts();
            CreateOutPorts();
            SetStyle();
        }

        private void CreateInPorts()
        {
            if (!NodeData.IsRoot())
                In = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));

            if (In != null)
            {
                // var label = In.Q<Label>();
                // if (label != null) In.Remove(label);
                In.portName = "";
                inputContainer.Add(In);
            }
        }

        private void CreateOutPorts()
        {
            switch (NodeData.ChildCapacity())
            {
                case 0: // Action & Conditionals
                    break;
                case 1: // Decorators
                    Out = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single,
                        typeof(bool));
                    break;
                case < 0 or > 1: // Composites and Anything else
                    Out = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi,
                        typeof(bool));
                    break;
            }
            
            if (Out != null)
            {
                // var label = Out.Q<Label>();
                // if (label != null) Out.Remove(label);
                Out.portName = "";
                outputContainer.Add(Out);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            //Debug.Log("SetPos");
            NodeData.Position = new Vector2(newPos.xMin, newPos.yMin);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }
        }


        #region Port

        public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type)
        {
            var port = base.InstantiatePort(orientation, direction, capacity, type);
            
            CreateNewPort(ref port);
            //SetPortColor(ref port);
            GetPortElements(port, out var connector, out var cap, out var label);
            SetPortClasses(ref port, ref connector, ref cap, ref label);
            SetPortHierarchy(ref port, connector, cap, label);
            
            // if (port.ClassListContains("output"))
            // {
            //     port.style.alignItems = new StyleEnum<Align>(Align.FlexEnd);
            // }
            // else
            // {
            //     port.style.alignItems = new StyleEnum<Align>(Align.FlexStart);
            // }
            // port.name = "port";
            // var label = port.Q<Label>();
            // label.focusable = false;
            
            
            //(EditorGUIUtility.Load(uiFile) as VisualTreeAsset).CloneTree((VisualElement) this);
            return port;
        }

        private void CreateNewPort(ref Port port)
        {
            GetPortElements(port, out var connector, out var cap, out var label);

            var portVisual = new VisualElement()
            {
                name = "port-visual"
            };
            portVisual.AddToClassList("port-visual");
            
            port.Add(portVisual);
        }

        private void SetPortColor(ref Port port)
        {
            port.portColor = Color.yellow;
        }

        private void GetPortElements(in Port port, out VisualElement connector, out VisualElement cap, out Label label)
        {
            connector = port.Q("connector");
            cap = connector.Q("cap");
            label = port.Q<Label>("type");
        }

        private void SetPortHierarchy(ref Port port, in VisualElement connector, in VisualElement cap, in Label label)
        {
            port.Remove(connector);
            port.Remove(label);
            
            port.Add(label);
            port.Add(connector);
        }

        private void SetPortClasses(ref Port port, ref VisualElement connector, ref VisualElement cap, ref Label label)
        {
            connector.RemoveFromClassList("connectorBox");
            cap.RemoveFromClassList("connectorCap");
            
            connector.AddToClassList("port-connector");
            cap.AddToClassList("port-connector-cap");
            
            
            label.RemoveFromClassList("unity-text-element");
            label.RemoveFromClassList("unity-label");
            label.RemoveFromClassList("connectorText");
            
            label.AddToClassList("port-label");
        }

        #endregion

        public void SetStyle()
        {
            switch (NodeData)
            {
                case ActionData:
                    AddToClassList(ActionClass);
                    break;
                case ConditionalData:
                    AddToClassList(ConditionalClass);
                    break;
                case DecoratorData:
                    AddToClassList(DecoratorClass);
                    break;
                case CompositeData:
                    AddToClassList(CompositeClass);
                    break;
                case RootData:
                    AddToClassList(RootClass);
                    break;
                default:
                    break;
            }
        }
        
    }
}