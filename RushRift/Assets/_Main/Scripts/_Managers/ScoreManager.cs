using UnityEngine;
using Game.DesignPatterns.Observers;
using Game;
using Game.Entities;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int playerCurrency;
    private IObserver<int> _onPointsGainObserver;

    private void Start()
    {
        _onPointsGainObserver = new ActionObserver<int>(OnPointsGain);

        EnemyController.OnEnemyGivesPoints.Attach(_onPointsGainObserver);
        scoreText.text = playerCurrency.ToString();
    }

    public void OnPurchase(int cost)
    {
        if (playerCurrency < cost) return;
        playerCurrency -= cost;
        if (playerCurrency < 0) playerCurrency = 0;
        scoreText.text = playerCurrency.ToString();
    }

    private void OnPointsGain(int points)
    {
        Debug.Log("sume puntos");
        playerCurrency += points;
        scoreText.text = playerCurrency.ToString();
    }
}
