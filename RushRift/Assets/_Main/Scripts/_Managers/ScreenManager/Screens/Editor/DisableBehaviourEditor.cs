using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(DisableBehaviour))]
public class DisableBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Get Behaviours"))
        {
            var disableBehaviour = target as DisableBehaviour;
            
            Undo.RecordObject(disableBehaviour, "Set Disable Behaviours");
            
            if (disableBehaviour != null && disableBehaviour.TrySetBehaviour(disableBehaviour.GetComponents<Behaviour>()))
            {
                EditorUtility.SetDirty(disableBehaviour);

                if (PrefabUtility.IsPartOfPrefabInstance(disableBehaviour))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(disableBehaviour);
                }
                
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(disableBehaviour.gameObject.scene);
                }
            }
        }
    }
}
