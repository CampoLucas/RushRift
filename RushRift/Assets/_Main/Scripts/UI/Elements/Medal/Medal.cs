using System;
using System.Collections;
using System.Collections.Generic;
using MyTools.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Medal : MonoBehaviour
{
    [SerializeField] private Graphic icon;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Graphic lockIcon;
    
    [Header("Materials")]
    [SerializeField] private Material lockedMaterial;
    [SerializeField] private Material unlockedMaterial;

    //private static readonly int UseLines = Shader.PropertyToID("_UseLines");

    public void Init(float time, bool unlocked)
    {
        text.text = time.FormatToTimer();

        if (!unlocked)
        {
            icon.material = lockedMaterial;
            lockIcon.gameObject.SetActive(true);
        }
        else
        {
            icon.material = unlockedMaterial;
            lockIcon.gameObject.SetActive(false);
        }
    }
}
