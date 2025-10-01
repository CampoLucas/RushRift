using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class TimerFormatter
{
    public static int[] GetNewTimer(float time)
    {
        int[] aux = new int[3];
        aux[0] = Mathf.FloorToInt(time / 60);
        aux[1] = Mathf.FloorToInt(time % 60);
        aux[2] = Mathf.FloorToInt((time % 1) * 1000);

        return aux;
    }

    public static void FormatTimer(TMP_Text text, int minutes, int seconds, int miliseconds)
    {
        if (!text)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: The passed TMP_Text is null. Returning.");
#endif
            return;
        }
        text.text = string.Format("{0:0}:{1:00}.{2:00}", minutes, seconds, miliseconds);
    }
}
