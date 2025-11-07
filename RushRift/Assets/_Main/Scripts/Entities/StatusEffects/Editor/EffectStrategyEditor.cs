using UnityEditor;

namespace Game.Entities.Editor
{
    [CustomEditor(typeof(EffectStrategy), true)]
    public class EffectStrategyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var effectStrategy = target as EffectStrategy;

            if (effectStrategy.Description() != "")
            {
                EditorGUILayout.HelpBox(effectStrategy.Description(), MessageType.Info);
            }
        }
    }
}