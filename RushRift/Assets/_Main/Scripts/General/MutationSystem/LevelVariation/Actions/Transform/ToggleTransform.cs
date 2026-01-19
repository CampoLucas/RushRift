using System;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Actions/Toggle Transform")]
    public class ToggleTransform : VariationAction
    {
        [SerializeField] private Transform target, successPos, failurePos;
        [SerializeField] private bool scaleTarget = false;

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
            SetTargetTransform(successPos);
        }

        private void Undo()
        {
            SetTargetTransform(failurePos);
        }

        private void SetTargetTransform(Transform other)
        {
            target.position = other.position;
            target.rotation = other.rotation;
            if (scaleTarget) target.localScale = target.InverseTransformVector(other.lossyScale);
        }
    }
}