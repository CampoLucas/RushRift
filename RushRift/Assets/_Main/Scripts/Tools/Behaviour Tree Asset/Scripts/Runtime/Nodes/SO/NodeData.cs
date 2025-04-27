using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime.Interfaces;
using BehaviourTreeAsset.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime.Node
{
    public abstract class Node<TData> : INode where TData : INodeData
    {
        private const string _logTag = "BehaviourTreeAsset.Runtime.Node";
        
        #region Public Properties

        public NodeState CurrentState { get; private set; }
        public bool Started { get; private set; }
        public bool Enabled { get; private set; }
        public List<INode> Children { get; private set; } = new List<INode>();

        #endregion

        #region Protected Properties

        protected TData Data;
        protected GameObject Owner { get; private set; }
        protected Transform OwnerTransform { get; private set; }
        protected IBehaviour OwnerBehaviour { get; private set; }

        #endregion

        #region Private Properties

        private int _childIndex;

        #endregion

        #region Constructor

        public Node(TData data)
        {
            Data = data;
            SetChildren(data);
        }

        #endregion
        
        #region Public Methods

        /// <summary>
        /// Assigns the nodes references and calls the OnAwake method.
        /// </summary>
        /// <param name="owner">The owner of the behaviour tree.</param>
        public void DoAwake(GameObject owner, IBehaviour ownerBehaviour)
        {
            if (owner == null)
            {
                DebugLogger.EditorLog("The Owner GameObject in the DoAwake is null", null, DebugLogger.LogType.Error, _logTag);
                return;          
            }

            Owner = owner;
            OwnerTransform = Owner.transform;
            OwnerBehaviour = ownerBehaviour;
            OnAwake();
            if (GetChildCount() > 0)
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (child == null) continue;
                    child.DoAwake(owner, ownerBehaviour);
                }
            }
        }

        /// <summary>
        /// Updates the node's logic.
        /// </summary>
        /// <returns>The state of the node.</returns>
        public NodeState DoUpdate()
        {
            if (CurrentState == NodeState.Disable)
            {
                return NodeState.Disable;
            }

            if (!Started)
            {
                Started = true;
                OnStart();
            }

            if (TryFailure(out var message))
            {
#if UNITY_EDITOR
                Debug.LogWarning(message);
#endif
                CurrentState = NodeState.Failure;
            }
            else
            {
                CurrentState = OnUpdate();
            }

            if (CurrentState is NodeState.Failure or NodeState.Success)
            {
                Started = false;
                OnStop();
            }

            return CurrentState;
        }
        
        public void Reset()
        {
            Started = false;
            CurrentState = NodeState.Running;
            _childIndex = 0;
            OnReset();
        }
        
        // /// <summary>
        // /// Disables the node if it is enable.
        // /// </summary>
        // public void Disable()
        // {
        //     if (!Enabled) return;
        //     
        //     CurrentState = NodeState.Disable;
        //     OnNodeDisable();
        //     Enabled = false;
        // }
        //
        // /// <summary>
        // /// Enables the node if it was disabled.
        // /// </summary>
        // public void Enable()
        // {
        //     if (Enabled) return;
        //     
        //     CurrentState = NodeState.Running;
        //     OnNodeEnable();
        //     Enabled = true;
        // }

        /// <summary>
        /// Gets rid of all the references in the node.
        /// </summary>
        public void Dispose()
        {
            OnDispose();
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if (child == null)
                {
                    DebugLogger.EditorLog("Trying to dispose a null children node", null, DebugLogger.LogType.Warning, _logTag);
                    continue;
                }
                Children[i].Dispose();
            }
            Children.Clear();
            Children = null;

            Owner = null;
            OwnerTransform = null;
        }
        
        public INode GetChild()
        {
            switch (Data.ChildCapacity())
            {
                case 0:
                    return null;
                case 1:
                    return Children[0];
                default:
                    var child = Children[_childIndex];
                    if (_childIndex + 1 > Children.Count - 1) _childIndex = 0;
                    else _childIndex++;
                    return child;
            }
        }

        public void SetChildren(List<INode> newChildren)
        {
            Children = newChildren;
        }

        public void SetChildren(INodeData data)
        {
            var dataChildren = data.Children;
            var childrenCount = dataChildren.Count;
            var newChildren = new List<INode>(childrenCount);
            
            for (var i = 0; i < childrenCount; i++)
            {
                var dataChild = dataChildren[i];
                if (dataChild == null)
                {
                    DebugLogger.EditorLog("Trying to add a null child from a INodeData in the node. Continuing", null, DebugLogger.LogType.Warning, _logTag);
                    continue;
                }

                if (dataChild.TryCreateNode(out var n))
                {
                    newChildren.Add(n);
                }
                else
                {
                    DebugLogger.EditorLog("Trying to add a null child node.", null, DebugLogger.LogType.Warning, _logTag);
                }
            }

            Children = newChildren;
        }

        public int GetChildCount() => Children.Count;

        public int ChildCapacity() => Data.ChildCapacity();

        public INode GetChild(int index)
        {
            if (index >= Children.Count)
            {
                DebugLogger.EditorLog("Trying to get a child with a index greater than the children the node has.", Owner, DebugLogger.LogType.Warning, _logTag);
                return null;
            }

            return Children[index];
        }

        public bool TryGetChild(int index, out INode child)
        {
            child = GetChild(index);
            return child != null;
        }

        public bool IsRoot() => Data.IsRoot();
        public void SetBehaviour(IBehaviour behaviour)
        {
            OwnerBehaviour = behaviour;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Called after the node was initialized.
        /// </summary>
        protected virtual void OnAwake() {}
        
        /// <summary>
        /// Called when the node begins executing it's logic.
        /// </summary>
        protected virtual void OnStart() {}
        
        /// <summary>
        /// Called when the node finishes executing its logic.
        /// </summary>
        protected virtual void OnStop() {}

        /// <summary>
        /// The update loop of the node.
        /// It finishes executing when the state returns success or failure.
        /// </summary>
        /// <returns> The state of the node. </returns>
        protected virtual NodeState OnUpdate() => NodeState.Success;
        
        protected virtual void OnReset() {}
        
        /// <summary>
        /// Called before the node is destroyed.
        /// </summary>
        protected virtual void OnDispose() {}
        
        protected GameObject GetDefault(GameObject target)
        {
            return target ? target : Owner;
        }
        
        protected virtual bool TryFailure(out string message)
        {
            message = "";
            return false;
        }

        #endregion
        
    }
    
    public abstract class NodeData : ScriptableObject, INodeData
    {
        #region Public Properties

        public bool Enabled { get; }
        public string Name { get => name; set => name = value; }
        public string Description { get; private set; }

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                previousPosition = position;
                position = value;
                if (position != previousPosition)
                    OnPositionChange?.Invoke(position);
            }
        }

        public List<NodeData> Children => children;

        public Action<Vector2> OnPositionChange;
        
        [HideInInspector] public string guid;

        #endregion

        #region Private Properties

        [HideInInspector] [SerializeField] private List<NodeData> children = new(); //ToDo: make method that sets the list depending of the ChildAmount().
        [HideInInspector] [SerializeField] private Vector2 position;
        [HideInInspector] [SerializeField] private Vector2 previousPosition;
        [HideInInspector] [SerializeField] protected BehaviourTreeData behaviourData;

        #endregion

        #region Public methods

        public void Init(BehaviourTreeData data)
        {
            behaviourData = data;
        }
        
        public int GetChildCount() => children.Count;
        
        /// <summary>
        /// The amount of child nodes a node can have.
        /// -1 means unlimited.
        /// </summary>
        public virtual int ChildCapacity() => 0;
        

        public bool AddChild(NodeData nodeData)
        {
            switch (ChildCapacity())
            {
                case 0:
                    return false;
                case < 0:
                    return Add(nodeData);
                default:
                    if (children.Count < ChildCapacity())
                        return Add(nodeData);
                    return false;
            }
        }

        public bool RemoveChild(NodeData nodeData)
        {
            return Remove(nodeData);
        }

        public bool ContainsChild(NodeData nodeData)
        {
            return Children.Contains(nodeData);
        }

        public bool ContainsChildInChildren(NodeData nodeData)
        {
            // var result = false;
            // for (var i = 0; i < _children.Count; i++)
            // {
            //     var child = _children[i];
            //     if (child.GetChildCount() == 0) continue;
            //
            //     if (child.ContainsChild(node))
            //     {
            //         result = true;
            //         break;
            //     }
            //
            //     if (child.ContainsChildInChildren(node))
            //     {
            //         result = true;
            //         break;
            //     }
            // }

            // If current node contains the node returns true.
            if (ContainsChild(nodeData))
                return true;

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.ContainsChildInChildren(nodeData))
                    return true;
            }
            

            // If no node is found returns false.
            return false;
        }

        public INode CreateNode()
        {
            return OnCreateNode();
        }

        protected abstract INode OnCreateNode();

        public bool TryCreateNode(out INode node)
        {
            node = CreateNode();
            return node != null;
        }

        public void ArrangeChildren(Vector2 newPos)
        {
            //Debug.Log("Arrange");
            children = children.OrderBy(go => go.Position.x).ToList();
        }
        
        /// <summary>
        /// Determines if the node is a root node.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsRoot() => false;
        
        public virtual string NodeName() => GetType().Name;

        public virtual void OnDraw(Transform origin) { }
        public virtual void OnDrawSelected(Transform origin) { }
        
        #endregion
        
        #region Public methods
        
        /// <summary>
        /// Destroys the node.
        /// </summary>
        public void Destroy()
        {
            OnNodeDestroy();

            UnsubscribeToAllPositionChange();
            
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child != null)
                    child.Destroy();
            }

            children.Clear();
            children = null;
        }
        
        public void OnPopulateTree()
        {
            UnsubscribeToAllPositionChange();
            SubscribeToAllPositionChange();
        }

        #endregion
        
        #region Protected execution methods

        /// <summary>
        /// Called when the node is destroyed.
        /// </summary>
        protected virtual void OnNodeDestroy() {}

        #endregion

        #region Private Methods

        private bool Add(NodeData nodeData)
        {
            if (children.Contains(nodeData)) return false;
            nodeData.OnPositionChange += ArrangeChildren;
            children.Add(nodeData);
            
            ArrangeChildren(Vector2.zero);
            return true;
        }

        private bool Remove(NodeData nodeData)
        {
            if (!children.Contains(nodeData)) return false;
            nodeData.OnPositionChange -= ArrangeChildren;
            children.Remove(nodeData);

            ArrangeChildren(Vector2.zero);
            return true;
        }

        private void SubscribeToAllPositionChange()
        {
            if (children.Count <= 0) return;
            for (var i = 0; i < children.Count; i++)
            {
                children[i].OnPositionChange += ArrangeChildren;
            }
        }
        
        private void UnsubscribeToAllPositionChange()
        {
            if (children.Count <= 0) return;
            for (var i = 0; i < children.Count; i++)
            {
                children[i].OnPositionChange -= ArrangeChildren;
            }
        }

        #endregion
    }
}
