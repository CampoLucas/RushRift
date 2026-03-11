using System.Collections.Generic;
using Game.Dialogue.Predicate;
using Game.Entities.AttackSystem;
using MyTools.Global;
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
		
		this.Log("Call the dialogue");
		manager.dialogueBox.SetActive(true);
		manager.isDialogueActive = true;

		manager.lines.Clear();

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];

			if (line == null) continue;

			manager.lines.Enqueue(line);
		}

		manager.DisplayNextDialogueLine();

	}
}

