using System;
using Game.Utils;
using MyTools.Global;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Actions/Enable Object")]
    public class ObjectEnabler : VariationAction
    {
        [SerializeField] private GameObject[] targets;
        [SerializeField] private bool enable = true;

        protected override void OnInit()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                targets[i].SetActive(false);
            }
        }

        protected override void OnExecute(State state)
        {
            if (state == State.Success)
            {
                Do();
                return;
            }
            
            Undo();
        }

        private void Do()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var e = targets[i];
                if (e.IsNullOrMissingReference())
                {
                    this.Log($"Missing reference chief... {gameObject.name}", LogType.Error);
                    continue;
                }
                
                e.SetActive(enable);
            }
        }

        private void Undo()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var e = targets[i];
                if (e.IsNullOrMissingReference())
                {
                    this.Log($"Missing reference chief... {gameObject.name}", LogType.Error);
                    continue;
                }
                
                e.SetActive(!enable);
            }
        }
    }
}