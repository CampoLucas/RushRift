using System;
using System.Collections.Generic;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameModeElement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;
    [SerializeField] private List<Image> icons;

    private void Awake()
    {
        if (button) button.onClick.AddListener(() => { this.Log("Press button");});
    }

    public void Highlight()
    {
        
    }
}
