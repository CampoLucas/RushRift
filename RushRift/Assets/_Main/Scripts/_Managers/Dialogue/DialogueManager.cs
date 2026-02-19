using System.Collections;
using System.Collections.Generic;
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

	private Queue<DialogueLineSO> lines;

	public bool isDialogueActive = false;

	public float typingSpeed = 0.03f;
	public float dialogueDelay = 1f;


	private void Awake()
	{
		if (Instance == null)
			Instance = this;

		lines = new Queue<DialogueLineSO>();
	}

	public void StartDialogue(DialogueListSO dialogue)
	{
		Debug.Log("Llame al dialogo");
		dialogueBox.SetActive(true);
		isDialogueActive = true;

		lines.Clear();

		foreach (DialogueLineSO dialogueLine in dialogue.dialogueLines)
		{
			lines.Enqueue(dialogueLine);
		}

		DisplayNextDialogueLine();
	}

	public void DisplayNextDialogueLine()
	{
		if (lines.Count == 0)
		{
			EndDialogue();
			return;
		}

		DialogueLineSO currentLine = lines.Dequeue();

		//characterIcon.sprite = currentLine.characterIcon;
		characterName.text = currentLine.characterName;

		StopAllCoroutines();

		StartCoroutine(TypeSentence(currentLine));
		//StartCoroutine(PlayAudio(currentLine));

		
	}

	IEnumerator TypeSentence(DialogueLineSO dialogueLine)
	{
		dialogueArea.text = "";
		foreach (char letter in dialogueLine.line.ToCharArray())
		{
			dialogueArea.text += letter;
			yield return new WaitForSeconds(typingSpeed);
		}
		yield return new WaitForSeconds(dialogueDelay);
		DisplayNextDialogueLine();
	}

	IEnumerator PlayAudio(DialogueLineSO dialogueLine)
	{
		//play audio
		yield return new WaitForSeconds(dialogueLine.spokenLine.length);
	}


	void EndDialogue()
	{
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}
}
