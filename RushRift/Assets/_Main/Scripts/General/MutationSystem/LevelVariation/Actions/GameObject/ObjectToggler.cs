using System;
using Game.Utils;
using MyTools.Global;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Actions/Toggle Object")]
    public class ObjectToggler : VariationAction
    {
        [SerializeField] private GameObject[] success;
        [SerializeField] private GameObject[] failure;

        protected override void OnInit()
        {
            for (var i = 0; i < success.Length; i++)
            {
                success[i].SetActive(false);
            }

            for (var i = 0; i < failure.Length; i++)
            {
                failure[i].SetActive(false);
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
            ToggleObjects(true);
        }

        private void Undo()
        {
            ToggleObjects(false);
        }

        private void ToggleObjects(bool isActive)
        {
            for (var i = 0; i < success.Length; i++)
            {
                var e = success[i];
                if (e.IsNullOrMissing())
                {
                    this.Log("Missing reference chief...", LogType.Error);
                    continue;
                }
                e.SetActive(isActive);
            }

            for (var i = 0; i < failure.Length; i++)
            {
                var e = failure[i];
                if (e.IsNullOrMissing())
                {
                    this.Log("Missing reference chief...", LogType.Error);
                    continue;
                }
                e.SetActive(!isActive);
            }
        }
    }
}