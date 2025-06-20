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


    private void Awake()
    {
        dashDamagePerk.onClick.AddListener(() => OnPurchase(dashDamagePerk, dashDamageCost,dashDamageEffect));
        increaseCurrentEnergy.onClick.AddListener(() => OnPurchase(increaseCurrentEnergy, increaseCurrentEnergyCost,increaseCurrentEnergyEffect));
        dashCostText.text = dashDamageCost.ToString();
        increaseCurrentEnergyCostText.text =increaseCurrentEnergyCost.ToString();
        
    }

    private void Start()
    {
        var data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        scoreText.text = playerCurrency.ToString();

        if (dashDamagePerk == null)
        {
            Debug.Log("SuperTest: dashDamagePerk is null");
        }
        
        DisablePurchase(dashDamagePerk, dashDamageEffect);
        DisablePurchase(increaseCurrentEnergy, increaseCurrentEnergyEffect);
    }


    public void OnPurchase(Button thisButton, int cost, int perk)
    {
        var data = SaveAndLoad.Load();
        if (playerCurrency < cost) return;
        playerCurrency -= cost;
        data.playerCurrency = playerCurrency;
        if (playerCurrency < 0) playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();

        if (data.UnlockedEffects.ContainsKey(perk)) data.UnlockedEffects[perk] = true;
        else data.UnlockedEffects.Add(perk, true);

        SaveAndLoad.Save(data);
        DisablePurchase(thisButton, perk);

    }

    public void LoadLevel()
    {
        SceneManager.LoadScene("Level_1_Rework");
    }


    private void DisablePurchase(Button buttonToDisable, int perk)
    {
        var data = SaveAndLoad.Load();

        if (data == null)
        {
            Debug.Log("SuperTest: data is null");
        }
        else if (data.UnlockedEffects == null)
        {
            Debug.Log("SuperTest: data.UnlockedEffects is null");
        }
        
        if (!data.UnlockedEffects.ContainsKey(perk)) return;
        if (data.UnlockedEffects[perk] == true) buttonToDisable.interactable = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SceneManager.LoadScene("Level_1_ReRework");
    }
}
