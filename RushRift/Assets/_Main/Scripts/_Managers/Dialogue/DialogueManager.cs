using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using MyTools.Global;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
	public static DialogueManager Instance;

	//public Image characterIcon;
	public TMP_Text characterName;
	public TMP_Text dialogueArea;
	public GameObject dialogueBox;

	private Queue<Line> lines;

	public bool isDialogueActive = false;

	public float typingSpeed = 0.03f;
	public float dialogueDelay = 1f;


	private void Awake()
	{
		if (Instance == null)
			Instance = this;

		lines = new Queue<Line>();
	}

	public bool StartDialogue(DialogueContainerSO container)
	{
		if (container.IsNullOrMissing())
		{
			this.Log("Trying to execute a dialogue from a null or missing container.", LogType.Error);
		}
		
		this.Log("Call the dialogue");
		dialogueBox.SetActive(true);
		isDialogueActive = true;

		lines.Clear();

		var dialogues = container.Dialogues;
		DialogueSO dialogue = default;
		
		// Has to find the dialogue it can produce
		foreach (var d in dialogues)
		{
			if (d != null && d.CanExecute())
			{
				dialogue = (DialogueSO)d;
				break;
			}
		}

		if (dialogue == null)
		{
			return false;
		}

		for (var i = 0; i < dialogue.Lines.Length; i++)
		{
			var line = dialogue.Lines[i];

			if (line == null) continue;
			
			lines.Enqueue(line);
		}

		DisplayNextDialogueLine();
		return true;
	}

	public void DisplayNextDialogueLine()
	{
		if (lines.Count == 0)
		{
			EndDialogue();
			return;
		}

		var current = lines.Dequeue();

		//characterIcon.sprite = currentLine.characterIcon;
		characterName.text = "[Delete this]";

		StopAllCoroutines();

		StartCoroutine(TypeSentence(current));
		//StartCoroutine(PlayAudio(currentLine));

		
	}

	IEnumerator TypeSentence(Line line)
	{
		dialogueArea.text = "";
		foreach (char letter in line.Text.ToCharArray())
		{
			dialogueArea.text += letter;
			yield return new WaitForSeconds(typingSpeed);
		}
		yield return new WaitForSeconds(dialogueDelay);
		DisplayNextDialogueLine();
	}

	IEnumerator PlayAudio(Line line)
	{
		//play audio
		yield return new WaitForSeconds(line.Clip.length);
	}


	void EndDialogue()
	{
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}
}
