using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using MyTools.Global;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Game;

public struct AudioParameters
{
	public string speakerName;
	public string audioName;
	public float typingSpeed;
	public float dialogueDelay;
}

public class DialogueManager : MonoBehaviour
{
	public static DialogueManager Instance;

	//public Image characterIcon;
	public TMP_Text characterName;
	public TMP_Text dialogueArea;
	public GameObject dialogueBox;

	public Queue<Line> lines = new();
	public Queue<AudioParameters> audioParameters = new();

	public bool isDialogueActive = false;




	private void Awake()
	{
		//if (Instance == null)
  //      {
		//	Instance = this;
  //      }
  //      else
  //      {
		//	Destroy(gameObject);
  //      }
	}

	public bool ExecuteDialogue(DialogueContainerSO container)
	{
		if (container.IsNullOrMissing())
		{
			this.Log("Trying to execute a dialogue from a null or missing container.", LogType.Error);
		}

		if (isDialogueActive) return false;

		var dialogues = container.Dialogues;
		DialogueSO dialogue = default;

		// Has to find the dialogue it can produce
		foreach (var d in dialogues)
		{
			if (d != null && d.CanExecute())
			{
				dialogue = (DialogueSO)d;
				dialogue.Execute(this);
				dialogueBox.SetActive(true);
				//AudioManager.Play(dialogue.DialogueAudioName);
				break;
			}
		}


		if (dialogue == null)
		{
			return false;
		}

		return true;
	}


	public void DisplayNextDialogueLine()
	{
		if (lines.Count == 0)
		{
			EndDialogue();
			return;
		}

		var currentLine = lines.Dequeue();
		var currentAudioParameters = audioParameters.Dequeue();
		StopAllCoroutines();
		AudioManager.Play(currentAudioParameters.audioName);
		StartCoroutine(TypeSentence(currentLine, currentAudioParameters.speakerName,currentAudioParameters.typingSpeed, currentAudioParameters.dialogueDelay));		
	}

	IEnumerator TypeSentence(Line line, string speakerName, float typingSpeed, float dialogueDelay)
	{
		dialogueArea.text = "";
		characterName.text = speakerName;
		
		foreach (char letter in line.Text.ToCharArray())
		{
			dialogueArea.text += letter;
			yield return new WaitForSeconds(typingSpeed);
		}
		yield return new WaitForSeconds(dialogueDelay);
		DisplayNextDialogueLine();
	}

	


	void EndDialogue()
	{
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}
}
