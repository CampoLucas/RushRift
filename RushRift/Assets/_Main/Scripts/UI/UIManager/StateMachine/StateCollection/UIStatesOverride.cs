using System.Collections.Generic;
using Game.UI.StateMachine.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.StateMachine
{
    [CreateAssetMenu(menuName = "Game/UI/StatesOverride")]
    public class UIStatesOverride : UIStateCollection
    {
        public UIStateCollection Parent => parent;

        [SerializeField] private UIStateCollection parent;

        public override void Test()
        {
            var nullV = "NULL";
            var p = GetPresenters();
            
            foreach (var key in p.Keys)
            {
                var pName = presenters[key] != null ? presenters[key].name : nullV;
                
                Debug.Log($"[SuperTest] {name}. Key: {key}, Value {pName}");
            }
        }

        public override UIScreen GetRootScreen()
        {
            return Parent.GetRootScreen();
        }

        public override Dictionary<UIScreen, BaseUIPresenter> GetPresenters()
        {
            var merged = new Dictionary<UIScreen, BaseUIPresenter>();
            
            if (parent != null)
            {
                var parentPresenters = parent.GetPresenters();
                foreach (var kvp in parentPresenters)
                    merged[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in presenters)
            {
                if (kvp.Value != null)
                    merged[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        public override List<UITransitionDefinition> GetTransitions()
        {
            return parent != null ? parent.GetTransitions() : new List<UITransitionDefinition>();
        }
        
//         private void OnValidate()
//         {
// #if UNITY_EDITOR
//             if (parent == null)
//                 return;
//             
//
//             // 1. Make sure presenters exist
//             if (presenters == null)
//                 presenters = new SerializedDictionary<UIScreen, BaseUIPresenter>();
//
//             // 2. Get all parent presenters
//             var parentDict = parent.GetPresenters();
//             bool changed = false;
//
//             // 3. Add missing keys
//             foreach (var key in parentDict.Keys)
//             {
//                 if (!presenters.ContainsKey(key))
//                 {
//                     presenters.Add(key, null, false); // false => allow null
//                     changed = true;
//                 }
//             }
//
//             // 4. Remove orphaned keys (that don’t exist anymore in parent)
//             var keysToRemove = new List<UIScreen>();
//             foreach (var key in presenters.Keys)
//             {
//                 if (!parentDict.ContainsKey(key))
//                     keysToRemove.Add(key);
//             }
//             foreach (var key in keysToRemove)
//             {
//                 presenters.Remove(key);
//                 changed = true;
//             }
//
//             // 5. Mark asset dirty if something changed
//             if (changed)
//             {
//                 UnityEditor.EditorUtility.SetDirty(this);
//             }
// #endif
//         }
    }
}