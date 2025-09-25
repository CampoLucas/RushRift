using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Entities;
using Game;
using System;

public class ShopManager : MonoBehaviour
{
    private int _level1;
    private int _level2;
    private int _level3;
    private int currentSelectedLevel;
    private string level1Text = "Rush 1";
    private string level2Text = "Old 1";
    private string level3Text = "Old 2";

    [Header("Medal Adquired")]
    [SerializeField] private Image[] level1Adquired; 
    [SerializeField] private Image[] level2Adquired; 
    [SerializeField] private Image[] level3Adquired;

    [Header("Level Best Time")]
    [SerializeField] private TMP_Text level1BestTime;
    [SerializeField] private TMP_Text level2BestTime;
    [SerializeField] private TMP_Text level3BestTime;

    [Header("Level Buttons")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;

    [Header("Gate Text")]
    [SerializeField] private TMP_Text gate;
    
    private void Awake()
    {
        level1Button.onClick.AddListener(() => OnSelectLevel(_level1, level1Text));
        /*level2Button.onClick.AddListener(() => OnSelectLevel(_level2, level2Text));
        level3Button.onClick.AddListener(() => OnSelectLevel(_level3, level3Text));*/
        _level1 = SceneManager.GetActiveScene().buildIndex + 1;
        /*_level2 = SceneManager.GetActiveScene().buildIndex + 2;
        _level3 = SceneManager.GetActiveScene().buildIndex + 3;*/
        currentSelectedLevel = _level1;

    }

    private void OnSelectLevel(int level, string levelName)
    {
        currentSelectedLevel = level;
        gate.text = levelName;
    }

    private void Start()
    {
        var data = SaveAndLoad.Load();

        if(data.BestTimes.Count != 0)
        {
            int[] _newTimer;
            if (data.BestTimes.ContainsKey(_level1))
            {
                _newTimer = TimerFormatter.GetNewTimer(data.BestTimes[_level1]);
                TimerFormatter.FormatTimer(level1BestTime, _newTimer[0], _newTimer[1], _newTimer[2]);
            }

            if (data.BestTimes.ContainsKey(_level2))
            {
                _newTimer = TimerFormatter.GetNewTimer(data.BestTimes[_level2]);
                TimerFormatter.FormatTimer(level2BestTime, _newTimer[0], _newTimer[1], _newTimer[2]);
            }

            if (data.BestTimes.ContainsKey(_level3))
            {
                _newTimer = TimerFormatter.GetNewTimer(data.BestTimes[_level3]);
                TimerFormatter.FormatTimer(level3BestTime, _newTimer[0], _newTimer[1], _newTimer[2]);
            }
            
        }

        if (data.LevelsMedalsTimes.Count != 0)
        {
            var medalTimes = data.LevelsMedalsTimes;

            if (medalTimes[_level1].bronze.isAcquired) level1Adquired[0].enabled = true;
            if (medalTimes[_level1].silver.isAcquired) level1Adquired[1].enabled = true;
            if (medalTimes[_level1].gold.isAcquired) level1Adquired[2].enabled = true;

            /*if (medalTimes[_level2].bronze.isAcquired) level2Adquired[0].enabled = true;
            if (medalTimes[_level2].silver.isAcquired) level2Adquired[1].enabled = true;
            if (medalTimes[_level2].gold.isAcquired) level2Adquired[2].enabled = true;

            if (medalTimes[_level3].bronze.isAcquired) level3Adquired[0].enabled = true;
            if (medalTimes[_level3].silver.isAcquired) level3Adquired[1].enabled = true;
            if (medalTimes[_level3].gold.isAcquired) level3Adquired[2].enabled = true;*/
        }


        var medals = LevelManager.GetMedals();

        foreach (var item in medals)
        {
            if (data.LevelsMedalsTimes.ContainsKey(item.levelNumber)) continue;
            data.LevelsMedalsTimes.Add(item.levelNumber, item.levelMedalTimes);
        }

        SaveAndLoad.Save(data);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SceneManager.LoadScene(currentSelectedLevel);
    }
}