using UnityEngine;
using Game.DesignPatterns.Observers;
using Game;
using Game.Entities;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int playerCurrency;
    private SaveData data;
    private IObserver<int> _onPointsGainObserver;

    private void Start()
    {
        _onPointsGainObserver = new ActionObserver<int>(OnPointsGain);

        EnemyController.OnEnemyGivesPoints.Attach(_onPointsGainObserver);
        WinTrigger.OnWinGivePoints.Attach(_onPointsGainObserver);
        data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        else data = new();
        scoreText.text = playerCurrency.ToString();
    }


    public void OnPointsGain(int points)
    {
        playerCurrency += points;
        data.playerCurrency = playerCurrency;
        scoreText.text = playerCurrency.ToString();
        SaveAndLoad.Save(data);
    }
}
