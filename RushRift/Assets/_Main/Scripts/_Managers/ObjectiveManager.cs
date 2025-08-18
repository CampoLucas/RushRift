using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;
using Game.Entities;
using UnityEngine.SceneManagement;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    //[SerializeField] private TMP_Text totalEnemiesText;
    [SerializeField] private TMP_Text currentEnemiesText;


    private float _timer;
    private bool _triggered;
    private bool stopTimer;
    private int _currentEnemies = 0;
    //private int _totalEnemies = 0;
    private int[] _newTimer = new int[3];



    private IObserver _decreaseObserver;
    private IObserver _increaseObserver;
    private IObserver _onWinLevelObserver;


    private void Awake()
    {
        _decreaseObserver = new ActionObserver(DecreseEnemyQuantity);
        _increaseObserver = new ActionObserver(EnemyQuantity);
        _onWinLevelObserver = new ActionObserver(OnWinLevel);

        WinTrigger.OnWinSaveTimes.Attach(_onWinLevelObserver);
        EnemyController.OnEnemyDeathSubject.Attach(_decreaseObserver);
        EnemyController.OnEnemySpawnSubject.Attach(_increaseObserver);

        stopTimer = false;
        var data = SaveAndLoad.Load();
    }


    private void Update()
    {
        LevelTimer();     
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

    private void LevelTimer()
    {
        if (!stopTimer)
        {
            _timer += Time.deltaTime;
            _newTimer = TimerFormatter.GetNewTimer(_timer);
            TimerFormatter.FormatTimer(timerText, _newTimer[0], _newTimer[1], _newTimer[2]);
        }
    }

    

    private void OnWinLevel()
    {
        if (_triggered) return;
        _triggered = true;
        
        stopTimer = true;

        LevelManager.SetLevelCompleteTime(_timer);

    }
    
}
