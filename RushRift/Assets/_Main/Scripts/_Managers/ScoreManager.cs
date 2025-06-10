using UnityEngine;
using Game.DesignPatterns.Observers;
using Game;
using Game.Entities;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int currentPoints;
    private int playerCurrency;
    private SaveData data;
    private IObserver<int> _onPointsGainObserver;
    private IObserver<int> _onWinLevelObserver;

    private void Start()
    {
        _onPointsGainObserver = new ActionObserver<int>(OnPointsGain);
        _onWinLevelObserver = new ActionObserver<int>(OnWinLevel);

        EnemyController.OnEnemyGivesPoints.Attach(_onPointsGainObserver);
        WinTrigger.OnWinGivePoints.Attach(_onWinLevelObserver);
        data = SaveAndLoad.Load();
        if (data != null) playerCurrency = data.playerCurrency;
        else data = new();
        scoreText.text = currentPoints.ToString();
    }


    public void OnPointsGain(int points)
    {
        currentPoints += points;
        playerCurrency += currentPoints;
        data.playerCurrency = playerCurrency;
        scoreText.text = currentPoints.ToString();
        
    }

    public void OnWinLevel(int points)
    {
        OnPointsGain(points);
        SaveAndLoad.Save(data);
    }
}
