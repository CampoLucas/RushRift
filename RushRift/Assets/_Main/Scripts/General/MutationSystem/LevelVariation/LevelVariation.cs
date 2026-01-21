using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Utils;
using MyTools.Global;
using Unity.VisualScripting;
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
        [Tooltip("Actions that depending of the condition state does different things")]
        [SerializeField] private VariationAction[] actions;
        [Tooltip("Actions that are only executed if it returns success")]
        [SerializeField] private VariationAction[] success;
        [Tooltip("Actions that are only executed if it returns failure")]
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

        private async void Init()
        {
            var level = GlobalLevelManager.CurrentLevel.Get();
            if (!level)
            {
                await UniTask.WaitUntil(() => GlobalLevelManager.CurrentLevel.TryGet(out level));
            }

            if (!level.VariationsEnabled)
            {
                Destroy(this);
                return;
            }
            
            var condition = CheckConditions();

            var a = new List<VariationAction>();
            if (actions is { Length: > 0 }) a.AddRange(actions);
            if (success is { Length: > 0 }) a.AddRange(success);
            if (failure is { Length: > 0 }) a.AddRange(failure);

            for (var i = 0; i < a.Count; i++)
            {
                var action = a[i];
                if (!action)
                {
                    this.Log("The action is null, check if there isn't a null element in the lists.", LogType.Error);
                    continue;
                }
                else if (action.IsNullOrMissingReference())
                {
                    this.Log("The action is missing reference, check if there an action wasn't deleted.", LogType.Error);
                    continue;
                }
                action.Init();
            }
            
            ExecuteActions(actions, condition);
            
            if (condition == VariationAction.State.Success) {
                ExecuteActions(success, VariationAction.State.Success);
            }
            else {
                ExecuteActions(failure, VariationAction.State.Success);
            }
        }

        private void ExecuteActions(VariationAction[] actions, VariationAction.State state) {
            for (var i = 0; i < actions.Length; i++) {
                actions[i].Execute(state);
            }
        }

        private VariationAction.State CheckConditions() {
            var successCount = 0;

            for (var i = 0; i < conditions.Length; i++) {
                if (conditions[i].Evaluate()) {
                    successCount++;
                }
            }

            var required = GetRequiredSuccessCount();

            var result = evaluate switch
            {
                CompareType.NoneSuccess => successCount == 0,
                _ => successCount >= required
            };

            return result ? VariationAction.State.Success : VariationAction.State.Failure;
        }
    }
}
