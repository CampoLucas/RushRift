using System.Collections.Generic;
using BehaviourTreeAsset.Interfaces;
using BehaviourTreeAsset.Runtime.Node;
using UnityEditor;
using UnityEngine;

namespace BehaviourTreeAsset.Runtime
{
    [CreateAssetMenu()]
    public class BehaviourTreeData : ScriptableObject, IBehaviourData
    {
        #region Public properties

        public NodeData Root => root;
        public List<NodeData> Nodes => nodes;

        #endregion

        #region Private properties

        [HideInInspector] [SerializeField] private NodeData root;
        [HideInInspector] [SerializeField] private List<NodeData> nodes = new();

        #endregion
        
        public IBehaviour CreateBehaviour(GameObject owner, BehaviourTreeRunner runner)
        {
            var t = new BehaviourTree(this, owner);
            t.SetRunner(runner);
            return t;
        }

        public IBehaviour CreateBehaviour(GameObject owner)
        {
            throw new System.NotImplementedException();
        }

        public void OnPopulateView()
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].OnPopulateTree();
            }
        }
        
        public void SetRootNode(NodeData newNodeData)
        {
            root = newNodeData;
        }
        
        public NodeData CreateNode(System.Type type)
        {
            var node = CreateInstance(type) as NodeData;
            if (node != null)
            {
                node.Name = node.NodeName();
#if UNITY_EDITOR
                node.guid = GUID.Generate().ToString();
#endif
                nodes.Add(node);
                
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(node, this);
                AssetDatabase.SaveAssets();
#endif
            }

            node.Init(this);
            return node;
        }
        
        public void DeleteNode(NodeData nodeData)
        {
            nodeData.Destroy();
            Nodes.Remove(nodeData);
            
#if UNITY_EDITOR
            AssetDatabase.RemoveObjectFromAsset(nodeData);
            AssetDatabase.SaveAssets();
#endif
            
        }
        
        public bool AddChild(NodeData parent, NodeData child)
        {
            return parent.AddChild(child);
        }

        public bool RemoveChild(NodeData parent, NodeData child)
        {
            return parent.RemoveChild(child);
        }

        public List<NodeData> GetChildren(NodeData parent)
        {
            return parent.Children;
        }
    }
}