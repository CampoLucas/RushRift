using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using MyTools.Global;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Game;
using Game.Saves;
using Game.DesignPatterns.Observers;
using Game.UI;
using Game.Levels;

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
	public Image dialogueBoxImage;

	public Queue<Line> lines = new();
	public Queue<AudioParameters> audioParameters = new();

	public bool isDialogueActive = false;

	private ActionObserver<float> _onDialogueOpacityChanged;
	private ActionObserver<bool> _onSubtitlesEnabled;
	private ActionObserver<BaseLevelSO> _onLevelExit;
	private DialogueContainerSO currentDialogue;
	private string currentDialogueAudio;
	private bool _isSubtitlesEnabled;

	private void Awake()
	{
		var saveData = SaveSystem.LoadSettings();
		_isSubtitlesEnabled = saveData.Sound.isSubtitlesEnabled;
		SetDialogueOpacity(saveData.Sound.dialogueOpacity);
	}

    private void Start()
    {
		_onDialogueOpacityChanged = new ActionObserver<float>(OnDialogueOpacityChanged);
		_onSubtitlesEnabled = new ActionObserver<bool>(OnSubtitlesEnabled);
		_onLevelExit = new ActionObserver<BaseLevelSO>(OnLevelExitHandler);
		Options.SubtitlesEnabled.Attach(_onSubtitlesEnabled);
		Options.DialogueOpacityChanged.Attach(_onDialogueOpacityChanged);
		GameEntry.LoadingState.AttachOnPreload(_onLevelExit);
	}

    public bool ExecuteDialogue(DialogueContainerSO container)
	{
		if (container.IsNullOrMissing())
		{
			this.Log("Trying to execute a dialogue from a null or missing container.", LogType.Error);
		}

		if (isDialogueActive) return false;

		currentDialogue = container;
		var dialogues = container.Dialogues;
		DialogueSO dialogue = default;

		// Has to find the dialogue it can produce
		foreach (var d in dialogues)
		{
			if (d != null && d.CanExecute())
			{
				dialogue = (DialogueSO)d;
				dialogue.Execute(this);
                if (_isSubtitlesEnabled)
                {
					dialogueBox.SetActive(true);
				}		
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
		currentDialogueAudio = currentAudioParameters.audioName;
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
		var data = SaveSystem.LoadGame();
		data.SetDialogueHeard(currentDialogue.Dialogues[0].DialogueName);
		data.SaveGame();
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}

	private void OnDialogueOpacityChanged(float v) => SetDialogueOpacity(v);
	private void OnSubtitlesEnabled(bool v) => SetSubtitles(v);

	private void SetDialogueOpacity(float value)
    {
		var color = dialogueBoxImage.color;
		color.a = value;
		dialogueBoxImage.color = color;
    }

	private void SetSubtitles(bool v)
    {
		_isSubtitlesEnabled = v;
		dialogueBox.SetActive(v);
    }

	private void OnLevelExitHandler(BaseLevelSO level)
    {
		StopAllCoroutines();
		AudioManager.Stop(currentDialogueAudio);
		lines.Clear();
		audioParameters.Clear();
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}

    private void OnDestroy()
    {
		var dialogueOpacitySubject = Options.DialogueOpacityChanged;
		var subtitles = Options.SubtitlesEnabled;
		GameEntry.LoadingState.DetachOnPreload(_onLevelExit);

		if (_onDialogueOpacityChanged != null)
		{
			if (dialogueOpacitySubject != null) dialogueOpacitySubject.Detach(_onDialogueOpacityChanged);
			_onDialogueOpacityChanged.Dispose();
		}

		if (_onSubtitlesEnabled!= null)
		{
			if (subtitles != null) subtitles.Detach(_onSubtitlesEnabled);
			_onSubtitlesEnabled.Dispose();
		}

		StopAllCoroutines();
		AudioManager.Stop(currentDialogueAudio);
		lines.Clear();
		audioParameters.Clear();
		isDialogueActive = false;
		dialogueBox.SetActive(false);
	}
}
