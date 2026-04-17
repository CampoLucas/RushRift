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
	public float TypingSpeed => typingSpeed;
	public float DialogueDelay => dialogueDelay;

    
    [SerializeField] private DialogueLines[] dialogueLines;
	[SerializeField] private float typingSpeed = 0.03f;
	[SerializeField] private float dialogueDelay = 1f;
    
    
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
				nameAndAudio nameAndAudio;
				var currentLine = dialogueLines[i].audioAndLines[j].line;
				nameAndAudio.speakerName = dialogueLines[i].name;
				nameAndAudio.audioName = dialogueLines[i].audioAndLines[j].dialogueAudioName;

				if (currentLine == null) continue;

				manager.lines.Enqueue(currentLine);
				manager.names.Enqueue(nameAndAudio);
			}
			
		}

		manager.SetDialogueSpeedAndDelay(typingSpeed, dialogueDelay);
		manager.DisplayNextDialogueLine();

	}
}

