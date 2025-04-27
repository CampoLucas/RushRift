using BehaviourTreeAsset.Runtime.Nodes.SubBehaviour;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI
{
    [CustomEditor(typeof(RunBehaviourData))]
    public class RunBehaviourDataEditor : NodeEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var node = target as RunBehaviourData;
            var hasSubTree = node.TryGetSubTree(out var st);
            if (hasSubTree)
            {
                if (GUILayout.Button("Open"))
                {
                    BehaviourTreeWindow.OpenSubTree(st);
                }
            }
        }
    }
}