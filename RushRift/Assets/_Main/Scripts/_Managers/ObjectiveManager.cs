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
    private int _minutes;
    private int _seconds;
    private int _miliSeconds;
    private SaveData data;


    private IObserver _decreaseObserver;
    private IObserver _increaseObserver;
    private IObserver<int> _onWinLevelObserver;


    private void Awake()
    {
        _decreaseObserver = new ActionObserver(DecreseEnemyQuantity);
        _increaseObserver = new ActionObserver(EnemyQuantity);
        _onWinLevelObserver = new ActionObserver<int>(OnWinLevel);

        WinTrigger.OnWinGivePoints.Attach(_onWinLevelObserver);
        EnemyController.OnEnemyDeathSubject.Attach(_decreaseObserver);
        EnemyController.OnEnemySpawnSubject.Attach(_increaseObserver);

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
            _minutes = GetMinutes(_timer);
            _seconds = GetSeconds(_timer);
            _miliSeconds = GetMiliseconds(_timer);
            FormatTimer(timerText);
        }
    }

    private int GetMinutes(float item)
    {
        return Mathf.FloorToInt(item / 60);
    }
    private int GetSeconds(float item)
    {
        return Mathf.FloorToInt(item % 60);
    }
    private int GetMiliseconds(float item)
    {
        return Mathf.FloorToInt((item % 1) * 1000);
    }

    private void FormatTimer(TMP_Text text)
    {
        text.text = string.Format("{0:00}:{1:00}:{2:000}", _minutes, _seconds, _miliSeconds);
    }

    private void OnWinLevel(int obj)
    {
        stopTimer = true;
        data = SaveAndLoad.Load();
        if (!data.levelBestTimes.ContainsKey(currentLevel))
        {
            Debug.Log("Me cree");
            data.levelBestTimes.TryAdd(currentLevel, _timer.ToString());
        }
        var aux = float.Parse(data.levelBestTimes[currentLevel]);
        Debug.Log(data.levelBestTimes[currentLevel]);
        if (aux > _timer) data.levelBestTimes[currentLevel] = _timer.ToString();
        bestTimerText.text = data.levelBestTimes[currentLevel].ToString();
        FormatTimer(bestTimerText);
        finalTimerText.text = timerText.text;
        FormatTimer(timerText);

        SaveAndLoad.Save(data);

    }
    
}
