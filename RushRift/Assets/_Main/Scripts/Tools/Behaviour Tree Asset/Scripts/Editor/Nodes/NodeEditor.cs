using System.Collections;
using System.Collections.Generic;
using BehaviourTreeAsset.Runtime;
using BehaviourTreeAsset.Runtime.Node;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI
{
    [CustomEditor(typeof(NodeData), true)]
    public class NodeEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            //var inspector = base.CreateInspectorGUI();
            var inspector = new VisualElement();
            
            var node = target as NodeData;
            var label = new Label(node.Name);

            
            inspector.Add(label);
            
            node.Name = label.text;
            
            return inspector;
        }
        
        public override void OnInspectorGUI()
        {
            //var node = target as Node;
            //var label = EditorGUI.TextField(node.Name, );
            
            base.OnInspectorGUI();
        }
    }

}