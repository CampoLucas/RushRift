using System.Collections.Generic;
using Game.Dialogue.Predicate;
using Game.Entities.AttackSystem;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct AudioAndLines
{
	public string dialogueAudioName;
	public float typingSpeed;
	public float dialogueDelay;
	public Line line;
}

[System.Serializable]
public struct DialogueLines
{
	public string name;
	public AudioAndLines[] audioAndLines;
}

public class DialogueSO : BaseDialogueSO
{
    [SerializeField] private DialogueLines[] dialogueLines;
    
    public override void Execute(DialogueManager manager)
    {
		// ToDo: Encapsulate the execution logic of the dialogue and do it here
		// so that if we have more types of dialogues, each can have their own way of executing 
		
		this.Log("Call the dialogue");
		manager.isDialogueActive = true;

		manager.lines.Clear();

		for (var i = 0; i < dialogueLines.Length; i++)
		{
			for (var j = 0; j < dialogueLines[i].audioAndLines.Length; j++)
            {
				AudioParameters audioParameters;
				var currentLine = dialogueLines[i].audioAndLines[j].line;
				audioParameters.speakerName = dialogueLines[i].name;
				audioParameters.audioName = dialogueLines[i].audioAndLines[j].dialogueAudioName;
				audioParameters.typingSpeed = dialogueLines[i].audioAndLines[j].typingSpeed;
				audioParameters.dialogueDelay = dialogueLines[i].audioAndLines[j].dialogueDelay;

				if (currentLine == null) continue;

				manager.lines.Enqueue(currentLine);
				manager.audioParameters.Enqueue(audioParameters);
			}
			
		}

		manager.DisplayNextDialogueLine();

	}
}

