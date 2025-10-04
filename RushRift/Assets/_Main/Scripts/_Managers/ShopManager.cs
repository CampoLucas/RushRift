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

    [Header("Level Buttons")]
    [SerializeField] private Button level1Button;

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
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SceneManager.LoadScene(currentSelectedLevel);
    }
}