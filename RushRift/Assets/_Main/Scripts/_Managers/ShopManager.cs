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
    [SerializeField] private TMP_Text increaseCurrentEnergyCostText;
    [SerializeField] private Button dashDamagePerk;
    [SerializeField] private Button increaseCurrentEnergy;
    [SerializeField] private int dashDamageEffect;
    [SerializeField] private int increaseCurrentEnergyEffect;
    [SerializeField] private int increaseCurrentEnergyCost;
    [SerializeField] private int dashDamageCost;
    private int playerCurrency;
    private SaveData data;

    private void Awake()
    {
        dashDamagePerk.onClick.AddListener(() => OnPurchase(dashDamagePerk, dashDamageCost,dashDamageEffect));
        increaseCurrentEnergy.onClick.AddListener(() => OnPurchase(increaseCurrentEnergy, increaseCurrentEnergyCost,increaseCurrentEnergyEffect));
        dashCostText.text = dashDamageCost.ToString();
        increaseCurrentEnergyCostText.text =increaseCurrentEnergyCost.ToString();
        
    }

    private void Start()
    {
        data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        else data = new();
        scoreText.text = playerCurrency.ToString();
        DisablePurchase(dashDamagePerk, dashDamageEffect);
        DisablePurchase(increaseCurrentEnergy, increaseCurrentEnergyEffect);
    }


    public void OnPurchase(Button thisButton, int cost, int perk)
    {
        if (playerCurrency < cost) return;
        playerCurrency -= cost;
        data.playerCurrency = playerCurrency;
        if (playerCurrency < 0) playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();

        if (data.unlockedEffects.ContainsKey(perk)) data.unlockedEffects[perk] = true;
        else data.unlockedEffects.Add(perk, true);

        DisablePurchase(thisButton, perk);
        SaveAndLoad.Save(data);

    }

    public void LoadLevel()
    {
        SceneManager.LoadScene("Level_1_Rework");
    }


    private void DisablePurchase(Button buttonToDisable, int perk)
    {
        if (!data.unlockedEffects.ContainsKey(perk)) return;
        if (data.unlockedEffects[perk] == true) buttonToDisable.interactable = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SceneManager.LoadScene("Level_1_Rework");
    }
}
