using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI;
using Game.UI.StateMachine;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class LevelLostTitle : MonoBehaviour
{
    [SerializeField] private TMP_Text tauntText;
    [SerializeField] private UIAnimation tauntAnim;
    [SerializeField] private string[] loseTaunts;

    [Header("Debug")]
    [SerializeField] private bool levelWon;

    

    public void Init()
    {
        tauntText.text = loseTaunts[Random.Range(0, loseTaunts.Length)];
        //tauntAnim.Play();
    }
}
