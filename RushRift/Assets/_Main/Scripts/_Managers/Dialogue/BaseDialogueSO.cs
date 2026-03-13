using Game.Dialogue.Predicate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseDialogueSO : ScriptableObject
{
    public string DialogueName => dialogueName;
    [SerializeField] private string dialogueName;
    [Header("Settings")]
    [SerializeField] private ExecutionFrequency frequency;

    // Once
    [SerializeField] private SavingMethod savingMethod; // ToDo: Only appears the option if frequency is Once

    // Random
    [SerializeField] private float chance; // ToDo: Only appears the option if frequency is random, is in percentage

    [Header("Conditions")]
    [Tooltip("All conditions must be met to play this dialogue.")]
    [SerializeField] private List<DialoguePredicate> conditions;

    public abstract void Execute(DialogueManager manager);

    public virtual bool CanExecute()
    {
        // ToDo: here it would check for the frequency.
        if (frequency == ExecutionFrequency.Once)
        {
            // ToDo: check it it was played before using the save file and return if it was.
        }
        else if (frequency == ExecutionFrequency.Random)
        {
            // ToDo: run a random roullete and if it didn't landed, return. 
        }

        for (var i = 0; i < conditions.Count; i++)
        {
            if (!conditions[i].Evaluate())
            {
                return false;
            }
        }

        return true;
    }
}
