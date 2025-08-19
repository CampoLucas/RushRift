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

        if (data.LevelsMedalsTimes.Count > 0)
        {
            Debug.Log($"Mi tiempo de bronze es: {data.LevelsMedalsTimes[1].bronze.time}");
            Debug.Log($"Mi medalla de bronze est� adquirida: {data.LevelsMedalsTimes[1].bronze.isAcquired}");
            Debug.Log($"Mi tiempo de silver es: {data.LevelsMedalsTimes[1].silver.time}");
            Debug.Log($"Mi medalla de silver est� adquirida: {data.LevelsMedalsTimes[1].silver.isAcquired}");
            Debug.Log($"Mi tiempo de gold es: {data.LevelsMedalsTimes[1].gold.time}");
            Debug.Log($"Mi medalla de gold est� adquirida: {data.LevelsMedalsTimes[1].gold.isAcquired}");
        }
        

        if (dashDamagePerk == null)
        {
            Debug.Log("SuperTest: dashDamagePerk is null");
        }

        DisablePurchase(dashDamagePerk, dashDamageEffect);
        DisablePurchase(increaseCurrentEnergy, increaseCurrentEnergyEffect);

        //if (data.LevelsMedalsTimes.Count != 0) return;  //linea para que solo cargue los tiempos una vez al arrancar el juego

        foreach (var item in ScriptableReference.Instance.medalReferences)
        {
            if (data.LevelsMedalsTimes.ContainsKey(item.levelNumber)) continue;
            data.LevelsMedalsTimes.Add(item.levelNumber, item.levelMedalTimes);
        }



        SaveAndLoad.Save(data);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
