using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.MutationSystem.LevelVariation
{
    /// <summary>
    /// A class that works like a branch, it decides what actions to execute based on the given conditions
    /// </summary>
    [AddComponentMenu("Game/Mutation System/Level Variation/Level Variation")]
    public class LevelVariation : MonoBehaviour
    {
        private enum CompareType {
            AllSuccess, // all conditions are true
            NoneSuccess, // None of the conditions are true
            OneSuccess, // At least one condition are true
            NSuccess // at least N conditions are true
        }
        
        [Header("Settings")]
        [Tooltip("How many conditions have to be successful for it to branch.\n"
            + "All = All conditions are true.\n"
            + "None = None of the conditions are true.\n"
            + "One = Only ones needs to be true.\n"
            + "N = At least N conditions are true."
            )]
        [SerializeField] private CompareType evaluate = CompareType.AllSuccess;
        [SerializeField] private int successAmount;
        
        [Header("Conditions")]
        [SerializeField] private VariationCondition[] conditions;
        
        [Header("Branching")]
        [SerializeField] private VariationAction[] success;
        [SerializeField] private VariationAction[] failure;

        private void Start()
        {
            Init();
        }

        private int GetRequiredSuccessCount()
        {
            return evaluate switch
            {
                CompareType.OneSuccess => 1,
                CompareType.NoneSuccess => 0,
                CompareType.AllSuccess => conditions.Length,
                CompareType.NSuccess => Mathf.Clamp(successAmount, 0, conditions.Length),
                _ => 0
            };
        }

        private void Init() {
            if (CheckConditions()) {
                ExecuteActions(success);
            }
            else {
                ExecuteActions(failure);
            }
        }

        private void ExecuteActions(VariationAction[] actions) {
            for (var i = 0; i < actions.Length; i++) {
                actions[i].Do();
            }
        }

        private bool CheckConditions() {
            var successCount = 0;

            for (var i = 0; i < conditions.Length; i++) {
                if (conditions[i].Evaluate()) {
                    successCount++;
                }
            }

            var required = GetRequiredSuccessCount();

            return evaluate switch
            {
                CompareType.NoneSuccess => successCount == 0,
                _ => successCount >= required
            };
        }
    }
}
