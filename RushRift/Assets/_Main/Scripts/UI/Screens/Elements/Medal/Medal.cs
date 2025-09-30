using System.Collections;
using System.Collections.Generic;
using MyTools.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Medal : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;

    public void Init(float time)
    {
        text.text = time.FormatToTimer();
    }
}
