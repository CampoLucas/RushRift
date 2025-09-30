using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game;
using Game.Entities;

namespace Game.UI.Screens
{
    public class LevelWonPresenter : UIPresenter<LevelWonModel, LevelWonView>
    {
        public int currentLevel => LevelManager.GetResolvedLevelNumber();

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hubButton;

        [Header("Timers")]
        [SerializeField] private TMP_Text current;
        [SerializeField] private TMP_Text best;
        [SerializeField] private TMP_Text final;
        [SerializeField] private GameObject newBest;

        [Header("Threshold Texts")]
        [SerializeField] private TMP_Text bronzeText;
        [SerializeField] private TMP_Text silverText;
        [SerializeField] private TMP_Text goldText;

        [Header("Icons")]
        [SerializeField] private Image bronzeAcquired;
        [SerializeField] private Image silverAcquired;
        [SerializeField] private Image goldAcquired;
        [SerializeField] private Sprite acquiredIcon;
        [SerializeField] private Sprite normalIcon;

        [Header("Effects UI")]
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
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;

            InitializeEffectRows();          // ← make sure rows start hidden
            InitializeThresholdTexts();      // ← you already added this
            GateContinueByBronze();
            ApplyWinFlow();
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
            if (currentIndex < sceneCount - 1) sceneToLoad = currentIndex + 1;
            SceneManager.LoadScene(sceneToLoad);
        }

        private void GateContinueByBronze()
        {
            var currentTime = LevelManager.LevelCompleteTime();

            if (!LevelManager.TryGetActiveMedal(out var medal) || medal == null)
            {
                continueButton.interactable = true;
                return;
            }

            continueButton.interactable = currentTime <= medal.levelMedalTimes.bronze.time;
        }
        
        private void InitializeEffectRows()
        {
            if (bronzeEffect) bronzeEffect.SetActive(false);
            if (silverEffect) silverEffect.SetActive(false);
            if (goldEffect)   goldEffect.SetActive(false);
        }
        
        private void InitializeThresholdTexts()
        {
            float bronzeTime, silverTime, goldTime;
            LevelMedalsSO medal;
            if (!LevelManager.TryGetActiveMedalTimes(out bronzeTime, out silverTime, out goldTime, out medal)) return;

            if (bronzeText)
            {
                var t = TimerFormatter.GetNewTimer(bronzeTime);
                TimerFormatter.FormatTimer(bronzeText, t[0], t[1], t[2]);
            }
            if (silverText)
            {
                var t = TimerFormatter.GetNewTimer(silverTime);
                TimerFormatter.FormatTimer(silverText, t[0], t[1], t[2]);
            }
            if (goldText)
            {
                var t = TimerFormatter.GetNewTimer(goldTime);
                TimerFormatter.FormatTimer(goldText, t[0], t[1], t[2]);
            }
        }
        
        private void ApplyWinFlow()
        {
            var runTime = LevelManager.LevelCompleteTime();

            float bronzeTime, silverTime, goldTime; LevelMedalsSO medal;
            if (!LevelManager.TryGetActiveMedalTimes(out bronzeTime, out silverTime, out goldTime, out medal) || medal == null)
            {
                ApplyBestAndUiOnly(runTime);
                return;
            }

            // Snapshot pre-state from SAVE to detect "newly earned this run"
            var dataBefore = SaveAndLoad.Load();
            var level = currentLevel;
            bool hadBronze = false, hadSilver = false, hadGold = false;
            if (dataBefore.LevelsMedalsTimes.TryGetValue(level, out var mtBefore))
            {
                hadBronze = mtBefore.bronze.isAcquired;
                hadSilver = mtBefore.silver.isAcquired;
                hadGold   = mtBefore.gold.isAcquired;
            }

            // Process: persist medal flags + apply upgrades immediately (centralized)
            UpgradeManager.ProcessLevelCompletion(runTime);

            // Reload to get post-state (source of truth for UI icons & “new” rows)
            var data = SaveAndLoad.Load();

            bool isNewBest = false;
            if (!data.BestTimes.ContainsKey(level)) { data.BestTimes.Add(level, runTime); isNewBest = true; }
            else if (data.BestTimes[level] > runTime) { data.BestTimes[level] = runTime; isNewBest = true; }

            var bestArr = TimerFormatter.GetNewTimer(data.BestTimes[level]);
            TimerFormatter.FormatTimer(best, bestArr[0], bestArr[1], bestArr[2]);
            if (newBest) newBest.SetActive(isNewBest);

            var curArr = TimerFormatter.GetNewTimer(runTime);
            TimerFormatter.FormatTimer(current, curArr[0], curArr[1], curArr[2]);
            TimerFormatter.FormatTimer(final,   curArr[0], curArr[1], curArr[2]);

            var brArr = TimerFormatter.GetNewTimer(bronzeTime);
            TimerFormatter.FormatTimer(bronzeText, brArr[0], brArr[1], brArr[2]);
            var siArr = TimerFormatter.GetNewTimer(silverTime);
            TimerFormatter.FormatTimer(silverText, siArr[0], siArr[1], siArr[2]);
            var goArr = TimerFormatter.GetNewTimer(goldTime);
            TimerFormatter.FormatTimer(goldText, goArr[0], goArr[1], goArr[2]);

            // Post-state flags (what we actually own now)
            var mtNow = data.LevelsMedalsTimes.TryGetValue(level, out var mtSaved) ? mtSaved : medal.levelMedalTimes;

            if (mtNow.bronze.isAcquired) bronzeAcquired.sprite = acquiredIcon; else bronzeAcquired.enabled = false;
            if (mtNow.silver.isAcquired) silverAcquired.sprite = acquiredIcon; else silverAcquired.enabled = false;
            if (mtNow.gold.isAcquired)   goldAcquired.sprite   = acquiredIcon; else goldAcquired.enabled = false;

            // Newly earned this run? Turn on rows + set texts
            bool gotBronzeNow = mtNow.bronze.isAcquired && !hadBronze;
            bool gotSilverNow = mtNow.silver.isAcquired && !hadSilver;
            bool gotGoldNow   = mtNow.gold.isAcquired   && !hadGold;

            if (gotBronzeNow)
            {
                if (bronzeEffectText) bronzeEffectText.text = mtNow.bronze.upgradeText;
                if (bronzeEffect) bronzeEffect.SetActive(true);
            }
            if (gotSilverNow)
            {
                if (silverEffectText) silverEffectText.text = mtNow.silver.upgradeText;
                if (silverEffect) silverEffect.SetActive(true);
            }
            if (gotGoldNow)
            {
                if (goldEffectText) goldEffectText.text = mtNow.gold.upgradeText;
                if (goldEffect) goldEffect.SetActive(true);
            }

            SaveAndLoad.Save(data);
        }
      
        private void ApplyBestAndUiOnly(float runTime)
        {
            var data = SaveAndLoad.Load();

            bool isNewBest = false;
            if (!data.BestTimes.ContainsKey(currentLevel))
            {
                data.BestTimes.Add(currentLevel, runTime);
                isNewBest = true;
            }
            else if (data.BestTimes[currentLevel] > runTime)
            {
                data.BestTimes[currentLevel] = runTime;
                isNewBest = true;
            }

            var bestArr = TimerFormatter.GetNewTimer(data.BestTimes[currentLevel]);
            TimerFormatter.FormatTimer(best, bestArr[0], bestArr[1], bestArr[2]);
            newBest.SetActive(isNewBest);

            var curArr = TimerFormatter.GetNewTimer(runTime);
            TimerFormatter.FormatTimer(current, curArr[0], curArr[1], curArr[2]);
            TimerFormatter.FormatTimer(final, curArr[0], curArr[1], curArr[2]);

            SaveAndLoad.Save(data);
        }

        private void TryApplyUpgrade(UpgradeEnum upgrade)
        {
            var effect = LevelManager.GetEffect(upgrade);
            if (effect) effect.ApplyEffect(null);
        }

        private void OnDestroy()
        {
            continueButton.onClick.RemoveAllListeners();
            retryButton.onClick.RemoveAllListeners();
            hubButton.onClick.RemoveAllListeners();
        }
    }
}