using System;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    [AddComponentMenu("Game/Mutation System/Level Variation/Actions/Enable")]
    public class EnableAction : VariationAction
    {
        [SerializeField] private GameObject[] targets;
        [SerializeField] private bool enable = true;
        public override void Do()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                targets[i].SetActive(enable);
            }
        }
    }
}