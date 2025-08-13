using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;
using Game.Entities;
using UnityEngine.SceneManagement;

public class ObjectiveManager : MonoBehaviour
{
    public int currentLevel => SceneManager.GetActiveScene().buildIndex;
    //[SerializeField] private int currentLevel;// horrible, no asignes el numero del nivel con un serialize field, no hay manera de saber que nivel es fuera de esta clase.
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text finalTimerText;
    [SerializeField] private TMP_Text bestTimerText;
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
            _newTimer = GetNewTimer(_timer);
            FormatTimer(timerText, _newTimer[0], _newTimer[1], _newTimer[2]);
        }
    }

    private int[] GetNewTimer(float time)
    {
        int[] aux = new int[3];
        aux[0] = Mathf.FloorToInt(time / 60);
        aux[1] = Mathf.FloorToInt(time % 60); 
        aux[2] = Mathf.FloorToInt((time % 1) * 1000);

        return aux;
    }

    private void FormatTimer(TMP_Text text, int minutes, int seconds, int miliseconds)
    {
        text.text = string.Format("{0:0}:{1:00}.{2:00}", minutes, seconds, miliseconds);
    }

    private void OnWinLevel()
    {
        if (_triggered) return;
        _triggered = true;
        
        stopTimer = true;
        
        LevelManager.SetLevelCompleteTime(_timer);
        
        var data = SaveAndLoad.Load();

        var medals = data.LevelsMedalsTimes[currentLevel];



        if (!data.BestTimes.ContainsKey(currentLevel)) data.BestTimes.Add(currentLevel, _timer);

        if (data.BestTimes[currentLevel] > _timer) data.BestTimes[currentLevel] = _timer;

        if (data.LevelsMedalsTimes[currentLevel].bronze.time > _timer) medals.bronze.isAcquired = true;

        if (data.LevelsMedalsTimes[currentLevel].silver.time > _timer) medals.silver.isAcquired = true;

        if (data.LevelsMedalsTimes[currentLevel].gold.time > _timer) medals.gold.isAcquired = true;

        data.LevelsMedalsTimes[currentLevel] = medals;

        Debug.Log($"Mi tiempo de bronze es: {data.LevelsMedalsTimes[1].bronze.time}");
        Debug.Log($"Mi medalla de bronze está adquirida: {data.LevelsMedalsTimes[1].bronze.isAcquired}");

        _newTimer = GetNewTimer(data.BestTimes[currentLevel]);
        FormatTimer(bestTimerText,_newTimer[0],_newTimer[1],_newTimer[2]);
        _newTimer = GetNewTimer(_timer);
        FormatTimer(finalTimerText, _newTimer[0], _newTimer[1], _newTimer[2]);

        SaveAndLoad.Save(data);

    }
    
}
