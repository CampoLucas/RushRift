using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int playerCurrency;
    private SaveData data;

    private void Start()
    {
        data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        else playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();
    }

    public void OnPurchase(int cost)
    {
        if (playerCurrency < cost) return;
        playerCurrency -= cost;
        if (playerCurrency < 0) playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();
        SaveAndLoad.Save(playerCurrency);
    }

    public void LoadLevel()
    {
        SceneManager.LoadScene("Level_1");
    }
}
