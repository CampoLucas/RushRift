using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class LevelWonPresenter : UIPresenter<LevelWonModel, LevelWonView>
    {
        public int currentLevel => SceneManager.GetActiveScene().buildIndex;
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hubButton;

        [Header("Timers")]
        [SerializeField] private TMP_Text current;
        [SerializeField] private TMP_Text best;
        [SerializeField] private GameObject newBest;
        [SerializeField] private TMP_Text bronzeText;
        [SerializeField] private TMP_Text silverText;
        [SerializeField] private TMP_Text goldText;

        [Header("Icons")]
        [SerializeField] private Image bronzeMedal;
        [SerializeField] private Image silverMedal;
        [SerializeField] private Image goldMedal;
        [SerializeField] private Image bronzeLocked;
        [SerializeField] private Image silverLocked;
        [SerializeField] private Image goldLocked;
        [SerializeField] private Sprite LockedIcon;

        [Header("Effects")]
        [SerializeField] private GameObject effectsContainer;
        [SerializeField] private TMP_Text upgradeText;
        [SerializeField] private TMP_Text bronzeEffectText;
        [SerializeField] private TMP_Text silverEffectText;
        [SerializeField] private TMP_Text goldEffectText;
        [SerializeField] private GameObject bronzeEffect;
        [SerializeField] private GameObject silverEffect;
        [SerializeField] private GameObject goldEffect;


        public override void Begin()
        {
            base.Begin();
            
            EventSystem.current.SetSelectedGameObject(null);
            
            // Set Cursor
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            CheckTime();
            OnWinLevel();
        }

        public override void End()
        {
            base.End();
            
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void Awake()
        {
            continueButton.onClick.AddListener(OnLoadNextHandler);
            retryButton.onClick.AddListener(RetryLevelHandler);
            hubButton.onClick.AddListener(HubLevelHandler);
            
        }

        private void HubLevelHandler()
        {
            SceneManager.LoadScene(0);
        }

        private void RetryLevelHandler()
        {
            var currentIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentIndex);
        }

        private void OnLoadNextHandler()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var currentIndex = SceneManager.GetActiveScene().buildIndex;

            var sceneToLoad = 0;

            
            if (currentIndex < sceneCount - 1)
            {
                sceneToLoad = currentIndex + 1;
            }

            SceneManager.LoadScene(sceneToLoad);
        }

        private void CheckTime()
        {
            var data = SaveAndLoad.Load();
            var currentTime = LevelManager.LevelCompleteTime();
            var currentIndex = SceneManager.GetActiveScene().buildIndex;


            if (currentTime > data.LevelsMedalsTimes[currentIndex].bronze.time)
            {
                continueButton.interactable = false;
                return;
            }

        }

        private void OnWinLevel()
        {
            var data = SaveAndLoad.Load();
            var medals = data.LevelsMedalsTimes[currentLevel];
            var time = LevelManager.LevelCompleteTime();
            int medalCount = 0;


            if (!data.BestTimes.ContainsKey(currentLevel)) data.BestTimes.Add(currentLevel, time);

            if (data.BestTimes[currentLevel] > time) data.BestTimes[currentLevel] = time;


            if (data.LevelsMedalsTimes[currentLevel].bronze.time > time && !data.LevelsMedalsTimes[currentLevel].bronze.isAcquired)
            {
                medals.bronze.isAcquired = true;
                bronzeEffectText.text = data.LevelsMedalsTimes[currentLevel].bronze.upgradeText;
                bronzeEffect.SetActive(true);
                medalCount++;
            }

            if (data.LevelsMedalsTimes[currentLevel].silver.time > time && !data.LevelsMedalsTimes[currentLevel].silver.isAcquired)
            {
                medals.silver.isAcquired = true;
                silverEffectText.text = data.LevelsMedalsTimes[currentLevel].silver.upgradeText;
                silverEffect.SetActive(true);
                medalCount++;
            }

            if (data.LevelsMedalsTimes[currentLevel].gold.time > time && !data.LevelsMedalsTimes[currentLevel].gold.isAcquired)
            {
                medals.gold.isAcquired = true;
                goldEffectText.text = data.LevelsMedalsTimes[currentLevel].gold.upgradeText;
                goldEffect.SetActive(true);
                medalCount++;
            }

            data.LevelsMedalsTimes[currentLevel] = medals;


            var _newTimer = TimerFormatter.GetNewTimer(data.BestTimes[currentLevel]);
            TimerFormatter.FormatTimer(best, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(time);
            TimerFormatter.FormatTimer(current, _newTimer[0], _newTimer[1], _newTimer[2]);

            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].bronze.time);
            TimerFormatter.FormatTimer(bronzeText, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].silver.time);
            TimerFormatter.FormatTimer(silverText, _newTimer[0], _newTimer[1], _newTimer[2]);
            _newTimer = TimerFormatter.GetNewTimer(data.LevelsMedalsTimes[currentLevel].gold.time);
            TimerFormatter.FormatTimer(goldText, _newTimer[0], _newTimer[1], _newTimer[2]);

            if (medals.bronze.isAcquired)
            {
                bronzeLocked.enabled = false;
                bronzeMedal.color = Color.white;
            } 

            if (medals.silver.isAcquired)
            {
                silverLocked.enabled = false;
                silverMedal.color = Color.white;
            } 
 
            if (medals.gold.isAcquired)
            {
                goldLocked.enabled = false;
                goldMedal.color = Color.white;
            } 
 

            if (medalCount > 0) effectsContainer.SetActive(true);
            if (medalCount > 1) upgradeText.text = "New Upgrades Unlocked";

            SaveAndLoad.Save(data);
        }

        private void OnDestroy()
        {
            continueButton.onClick.RemoveAllListeners();
            retryButton.onClick.RemoveAllListeners();
            hubButton.onClick.RemoveAllListeners();
        }
    }
}