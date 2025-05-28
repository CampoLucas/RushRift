using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Entities;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text dashCostText;
    [SerializeField] private Button dashDamagePerk;
    [SerializeField] private int dashDamageEffect;
    [SerializeField] private int dashDamageCost;
    private int playerCurrency;
    private SaveData data;

    private void Awake()
    {
        dashDamagePerk.onClick.AddListener(() => OnPurchase(dashDamageCost,dashDamageEffect));
        dashCostText.text = dashDamageCost.ToString();
    }

    private void Start()
    {
        data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        else data = new();
        scoreText.text = playerCurrency.ToString();
    }

    public void OnPurchase(int cost, int perk)
    {
        if (playerCurrency < cost) return;
        playerCurrency -= cost;
        data.playerCurrency = playerCurrency;
        if (playerCurrency < 0) playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();
        data.unlockedEffects = new Dictionary<int, bool>();

        if (data.unlockedEffects.ContainsKey(perk)) data.unlockedEffects[perk] = true;
        else data.unlockedEffects.Add(perk, true);

        SaveAndLoad.Save(data);
    }

    public void LoadLevel()
    {
        SceneManager.LoadScene("Level_1");
    }
}
