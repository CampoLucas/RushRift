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
        var manager = FindObjectOfType<DialogueManager>();
        if (manager.ExecuteDialogue(dialogue))
        {
            gameObject.SetActive(false);
        }
        //if (DialogueManager.Instance.ExecuteDialogue(dialogue))
        //{
        //    gameObject.SetActive(false);
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
        var data = SaveSystem.LoadGame();
        if (!other.CompareTag(triggerTag)) return;
        foreach (var item in dialogue.Dialogues)
        {
            if (item.CanExecute())
            {
                if (data.CheckDialogueHeard(item.DialogueName)) return;
                TriggerDialogue();
                if (dialogue.Dialogues[0].SavingMethod == SavingMethod.Never) return;
                //data.SetDialogueHeard(item.DialogueName);
                //data.SaveGame();
            }
        }
        
    }
}
