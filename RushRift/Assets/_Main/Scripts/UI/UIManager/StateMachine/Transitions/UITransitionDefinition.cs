using System.Linq;
using UnityEngine;

namespace Game.UI.StateMachine
{
    [System.Serializable]
    public class UITransitionDefinition
    {
        [SerializeField] private UIScreen from;
        [SerializeField] private bool screenTransition = true;
        [SerializeField] private UIScreen to;
        [SerializeField] private SceneTransition scene;

        [Header("Conditions")]
        [SerializeField] private SerializableSOCollection<UIPredicate> conditions;
        
        public void SetTransition(in UIStateMachine fsm)
        {
            if (screenTransition)
            {
                fsm.TrySetTransition(from, to, new CompositePredicate(conditions.ToArray()));
            }
            else
            {
                fsm.TrySetTransition(from, scene, new CompositePredicate(conditions.ToArray()));
            }
        }

        public void DestroyPredicates()
        {
#if UNITY_EDITOR
            if (conditions == null)
                return;

            // Copy to avoid modifying while iterating
            var predicates = conditions.ToArray();

            foreach (var predicate in predicates)
            {
                if (predicate == null)
                    continue;

                // Remove from asset
                var path = UnityEditor.AssetDatabase.GetAssetPath(predicate);
                if (!string.IsNullOrEmpty(path))
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(predicate);

                // Destroy in memory
                Object.DestroyImmediate(predicate, true);
            }

            conditions.Clear();
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}