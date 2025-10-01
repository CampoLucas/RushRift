using System.Collections;
using System.Collections.Generic;
using MyTools.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Medal : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image lockIcon;
    [SerializeField, Range(0, 1)] private float lockedIntensity = .25f;

    public void Init(float time, bool unlocked)
    {
        text.text = time.FormatToTimer();

        if (!unlocked)
        {
            var targetIconColor = icon.color * lockedIntensity; // 50% darker
            targetIconColor.a = icon.color.a;
            
            var targetTextColor = text.color * lockedIntensity; // optional if you want the text darker too
            targetTextColor.a = text.color.a;

            icon.color = targetIconColor;
            text.color = targetTextColor;

            lockIcon.gameObject.SetActive(true);
        }
        else
        {
            lockIcon.gameObject.SetActive(false);
        }
    }
}
