using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class LevelWonView : UIView
    {
        public int currentLevel => SceneManager.GetActiveScene().buildIndex;
        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text current;
        [SerializeField] private TMP_Text best;
        [SerializeField] private GameObject newBest;

        [SerializeField] private TMP_Text finalTimerText;
        [SerializeField] private TMP_Text bestTimerText;

        [SerializeField] private TMP_Text bronzeText;
        [SerializeField] private TMP_Text silverText;
        [SerializeField] private TMP_Text goldText;

        [SerializeField] private Image bronzeAcquired;
        [SerializeField] private Image silverAcquired;
        [SerializeField] private Image goldAcquired;

        [SerializeField] private Sprite acquiredIcon;
        [SerializeField] private Sprite normalIcon;

        protected override void Awake()
        {
            base.Awake();
            animator.enabled = false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            //animator.enabled = true;

            BestTime();
            OnWinLevel();
        }

        private void BestTime()
        {
            var data = SaveAndLoad.Load();
            if (data.BestTimes.TryGetValue(SceneManager.GetActiveScene().buildIndex, out var bestTime))
            {
                var time = LevelManager.LevelCompleteTime();

                //if (time <= bestTime) newBest.SetActive(true);
            }
        }

        private void OnWinLevel()
        {
            var data = SaveAndLoad.Load();
            var medals = data.LevelsMedalsTimes[currentLevel];
            var time = LevelManager.LevelCompleteTime();


            if (!data.BestTimes.ContainsKey(currentLevel)) data.BestTimes.Add(currentLevel, time);

            if (data.BestTimes[currentLevel] > time) data.BestTimes[currentLevel] = time;

            if (data.LevelsMedalsTimes[currentLevel].bronze.time > time) medals.bronze.isAcquired = true;

            if (data.LevelsMedalsTimes[currentLevel].silver.time > time) medals.silver.isAcquired = true;

            if (data.LevelsMedalsTimes[currentLevel].gold.time > time) medals.gold.isAcquired = true;

            data.LevelsMedalsTimes[currentLevel] = medals;


            var _newTimer = TimerFormatter.GetNewTimer(data.BestTimes[currentLevel]);
            TimerFormatter.FormatTimer(bestTimerText, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(time);
            TimerFormatter.FormatTimer(finalTimerText, _newTimer[0], _newTimer[1], _newTimer[2]);

            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].bronze.time);
            TimerFormatter.FormatTimer(bronzeText, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].silver.time);
            TimerFormatter.FormatTimer(silverText, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].gold.time);
            TimerFormatter.FormatTimer(goldText, _newTimer[0], _newTimer[1], _newTimer[2]);

            if (medals.bronze.isAcquired) bronzeAcquired.sprite = acquiredIcon;
            else bronzeAcquired.enabled = false;

            if (medals.silver.isAcquired) silverAcquired.sprite = acquiredIcon;
            else silverAcquired.enabled = false;

            if (medals.gold.isAcquired) goldAcquired.sprite = acquiredIcon;
            else goldAcquired.enabled = false;
          

            SaveAndLoad.Save(data);
        }
    }
}