using System.Collections.Generic;
using Game;
using Game.Entities.AttackSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue Container", menuName = "Game/Dialogue System/Dialogue Container")]
public class DialogueContainerSO : ScriptableObject
{
    public string Name => speakerName;
    public List<BaseDialogueSO> Dialogues => dialogues.Get();


    [Header("Speaker")]
    [SerializeField] private string speakerName;
    
    [Header("Dialogues")]
    [SerializeField] private SerializableSOCollection<BaseDialogueSO> dialogues;

}
