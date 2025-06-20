using UnityEngine;
using Game.DesignPatterns.Observers;
using Game;
using Game.Entities;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    public int CurrentPoints => currentPoints;
    
    [SerializeField] private TMP_Text scoreText;
    private int currentPoints;
    private int playerCurrency;
    private bool _triggered;
    private IObserver<int> _onPointsGainObserver;
    private IObserver<int> _onWinLevelObserver;

    private void Start()
    {
        _onPointsGainObserver = new ActionObserver<int>(OnPointsGain);
        _onWinLevelObserver = new ActionObserver<int>(OnWinLevel);

        EnemyController.OnEnemyGivesPoints.Attach(_onPointsGainObserver);
        WinTrigger.OnWinGivePoints.Attach(_onWinLevelObserver);
        scoreText.text = currentPoints.ToString();
    }


    public void OnPointsGain(int points)
    {
        currentPoints += points;
        playerCurrency += currentPoints;
        scoreText.text = currentPoints.ToString();
    }

    public void OnWinLevel(int points)
    {
        if (_triggered) return;
        _triggered = true;
        var data = SaveAndLoad.Load();
        OnPointsGain(points);
        data.playerCurrency += playerCurrency;
        SaveAndLoad.Save(data);

    }
}
