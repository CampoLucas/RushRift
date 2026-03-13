using Game.Saves;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string triggerTag = "Player";
    public DialogueContainerSO dialogue;


    public void TriggerDialogue()
    {
        if (DialogueManager.Instance.ExecuteDialogue(dialogue))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var data = SaveSystem.LoadGame();
        if (!other.CompareTag(triggerTag)) return;
        if (data.CheckDialogueHeard(dialogue.DialogueName)) return;
        TriggerDialogue();
        data.SetDialogueHeard(dialogue.DialogueName);
        data.SaveGame();

        gameObject.SetActive(false);
    }
}
