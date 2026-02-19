using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/UI/DialogueLine", order = 1)]
public class DialogueLineSO : ScriptableObject
{
    public string characterName;
    public Sprite characterIcon;

    [TextArea(3, 10)]
    public string line;

    public AudioClip spokenLine;
}

