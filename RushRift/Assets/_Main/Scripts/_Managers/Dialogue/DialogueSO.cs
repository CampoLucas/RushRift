using System.Collections.Generic;
using Game.Dialogue.Predicate;
using Game.Entities.AttackSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class DialogueSO : BaseDialogueSO
{
    public Line[] Lines => lines;
    
    [SerializeField] private Line[] lines;
    
    public override void Execute(DialogueManager manager)
    {
        // ToDo: Encapsulate the execution logic of the dialogue and do it here
        // so that if we have more types of dialogues, each can have their own way of executing 
    }
}

public abstract class BaseDialogueSO : ScriptableObject
{
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

public enum ExecutionFrequency
{
    Always, 
    Once, // ToDo: check on the save file
    Random // ToDo: roulette to see if executes the dialogue
}

/// <summary>
/// The dialogues that are only executed once, when are they saved to the save file
/// </summary>
public enum SavingMethod
{
    DialogueStart, DialogueEnded, OnLevelWon, OnLevelLost, OnLevelFinished
}

[System.Serializable]
public class Line
{
    public string Text => text;
    public AudioClip Clip => clip;

    [FormerlySerializedAs("line")]
    [TextArea(3, 10)]
    [SerializeField] private string text;
    [SerializeField] private AudioClip clip;
}