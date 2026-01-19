using System;
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
                success[i].SetActive(isActive);
            }

            for (var i = 0; i < failure.Length; i++)
            {
                failure[i].SetActive(!isActive);
            }
        }
    }
}