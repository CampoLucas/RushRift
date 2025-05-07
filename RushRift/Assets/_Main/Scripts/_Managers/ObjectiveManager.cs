using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;
using Game.Entities;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    //[SerializeField] private TMP_Text totalEnemiesText;
    [SerializeField] private TMP_Text currentEnemiesText;

    private float _timer;
    private int _currentEnemies = 0;
    private int _totalEnemies = 0;
    private int _minutes;
    private int _seconds;
    private int _miliSeconds;

    private IObserver _decreaseObserver;
    private IObserver _increaseObserver;

    private void Awake()
    {
        _decreaseObserver = new ActionObserver(DecreseEnemyQuantity);
        _increaseObserver = new ActionObserver(EnemyQuantity);

        EnemyController.onEnemyDeathSubject.Attach(_decreaseObserver);
        EnemyController.onEnemySpawnSubject.Attach(_increaseObserver);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        _minutes = Mathf.FloorToInt(_timer / 60);
        _seconds = Mathf.FloorToInt(_timer % 60);
        _miliSeconds = Mathf.FloorToInt((_timer % 1) * 1000);
        timerText.text = string.Format("{0:00}:{1:00}:{2:000}", _minutes,_seconds,_miliSeconds);

    }

    private void EnemyQuantity()
    {
        _currentEnemies++;
        //_currentEnemies = _totalEnemies;
        //totalEnemiesText.text = _totalEnemies.ToString();
        currentEnemiesText.text = _currentEnemies.ToString();
    }

    private void DecreseEnemyQuantity()
    {
        _currentEnemies--;
        currentEnemiesText.text = _currentEnemies.ToString();
    }
}
