using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string triggerTag = "Player";
    public DialogueContainerSO dialogue;


    public void TriggerDialogue()
    {
        if (DialogueManager.Instance.StartDialogue(dialogue))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;
        TriggerDialogue();

        gameObject.SetActive(false);
    }
}
