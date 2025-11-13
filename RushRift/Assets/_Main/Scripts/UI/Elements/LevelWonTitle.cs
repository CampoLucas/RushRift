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

public class LevelWonTitle : MonoBehaviour
{
    [SerializeField] private LevelWonPresenter presenter;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text tauntText;
    [SerializeField] private UIAnimation tauntAnim;

    [FormerlySerializedAs("winText")]
    [Header("On Win")]
    [SerializeField] private string wonText = "Level Completed!!!";
    [FormerlySerializedAs("winColor")] [SerializeField] private Color wonColor;

    [Header("On Lose")]
    [SerializeField] private string loseText = "Level Lost...";
    [SerializeField] private Color loseColor;
    [SerializeField] private string[] loseTaunts;

    [Header("Debug")]
    [SerializeField] private bool levelWon;
    
    public void Init()
    {
        var model = presenter.GetModel();
        var levelWon = model.LevelWon;


        text.text = levelWon ? wonText : loseText;
        text.color = levelWon ? wonColor : loseColor;
        if (!model.LevelWon)
        {
            tauntText.text = loseTaunts[Random.Range(0, loseTaunts.Length)];
            tauntText.gameObject.SetActive(true);
            tauntAnim.Play();
        }
        else
        {
            tauntText.gameObject.SetActive(false);
        }
    }
}
