using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData 
{
    public int playerCurrency;

    public SaveData(int score)
    {
        playerCurrency = score;
    }
}
