using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/UI/DialogueList", order = 2)]
public class DialogueListSO : ScriptableObject
{
    public List<DialogueLineSO> dialogueLines = new List<DialogueLineSO>();
}
