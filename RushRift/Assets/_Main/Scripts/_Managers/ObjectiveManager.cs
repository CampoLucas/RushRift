using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;
using Game.Entities;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private int currentLevel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text finalTimerText;
    [SerializeField] private TMP_Text bestTimerText;
    //[SerializeField] private TMP_Text totalEnemiesText;
    [SerializeField] private TMP_Text currentEnemiesText;

    private float _timer;
    private bool stopTimer;
    private int _currentEnemies = 0;
    //private int _totalEnemies = 0;
    private int[] _newTimer = new int[3];
    private int _minutes;
    private int _seconds;
    private int _miliSeconds;
    private SaveData data;


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

        data = SaveAndLoad.Load();
        stopTimer = false;
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
            _newTimer = GetNewTimer(_timer);
            FormatTimer(timerText, _newTimer[0], _newTimer[1], _newTimer[2]);
        }
    }

    private int[] GetNewTimer(float timer)
    {
        int[] aux = new int[3];
        aux[0] = Mathf.FloorToInt(timer / 60);
        aux[1] = Mathf.FloorToInt(timer % 60); 
        aux[2] = Mathf.FloorToInt((timer % 1) * 1000);

        return aux;
    }

    private void FormatTimer(TMP_Text text, int minutes, int seconds, int miliseconds)
    {
        text.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, miliseconds);
    }

    private void OnWinLevel()
    {
        stopTimer = true;
        
        if (!data.levelBestTimes.ContainsKey(currentLevel))
        {
            Debug.Log("Me cree");
            data.levelBestTimes.Add(currentLevel, _timer);
        }
        if (data.levelBestTimes[currentLevel] > _timer) data.levelBestTimes[currentLevel] = _timer; 
        Debug.Log(data.levelBestTimes[currentLevel]);
        _newTimer = GetNewTimer(data.levelBestTimes[currentLevel]);
        FormatTimer(bestTimerText,_newTimer[0],_newTimer[1],_newTimer[2]);
        _newTimer = GetNewTimer(_timer);
        FormatTimer(finalTimerText, _newTimer[0], _newTimer[1], _newTimer[2]);

        SaveAndLoad.Save(data);

    }
    
}
