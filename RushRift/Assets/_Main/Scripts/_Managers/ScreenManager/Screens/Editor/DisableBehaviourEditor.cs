using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DisableBehaviour))]
public class DisableBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Get Behaviours"))
        {
            var disableBehaviour = target as DisableBehaviour;
            disableBehaviour.SetBehaviour(disableBehaviour.GetComponents<Behaviour>());
        }
    }
}
