using System;
using Game.General;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public sealed class LevelWonPresenter : UIPresenter<LevelWonModel, LevelWonView>
    {
#if true
        public int CurrentLevel => SceneManager.GetActiveScene().buildIndex;
#else
        public int currentLevel => LevelManager.GetCurrentLevel();
#endif
        
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hubButton;

        [Header("Events")]
        [SerializeField] private UnityEvent onBegin = new UnityEvent();
        
        private void Awake()
        {
            continueButton.onClick.AddListener(OnLoadNextHandler);
            retryButton.onClick.AddListener(RetryLevelHandler);
            hubButton.onClick.AddListener(HubLevelHandler);
        }
        
        public override void Begin()
        {
            base.Begin();
            
            EventSystem.current.SetSelectedGameObject(null);
            
            // Set Cursor
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            //OnWinLevel();
            
            SetModelValues(Model);
            UpdateSaveData(Model);
            CheckTime(Model);
            
            onBegin?.Invoke();
        }

        public override void End()
        {
            base.End();
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        public LevelWonModel GetModel() => Model;

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

        private void CheckTime(in LevelWonModel model)
        {
            if (model.LevelWon) return;
            
            continueButton.interactable = false;
        }
        
        private void SetModelValues(in LevelWonModel model)
        {
            var data = SaveAndLoad.Load();
            var endTime = LevelManager.LevelCompleteTime();
            data.CheckBestTime(CurrentLevel, endTime, out var prevBest, out var currBest, out var newRecord);

            var bronze = LevelManager.GetMedalInfo(MedalType.Bronze);
            var silver = LevelManager.GetMedalInfo(MedalType.Silver);
            var gold = LevelManager.GetMedalInfo(MedalType.Gold);

            model.Initialize(endTime, currBest, newRecord, bronze, silver, gold);
        }

        private void UpdateSaveData(in LevelWonModel model)
        {
            var data = SaveAndLoad.Load();
            
            SaveUnlockedMedals(model, ref data);
            SaveNewBest(model, ref data);
            
            data.Save();
        }
        
        private void SaveUnlockedMedals(in LevelWonModel model, ref SaveData data)
        {
            var levelID = LevelManager.GetLevelID();
            
            if (model.IsMedalUnlocked(MedalType.Bronze)) data.UnlockMedal(levelID, MedalType.Bronze);
            if (model.IsMedalUnlocked(MedalType.Silver)) data.UnlockMedal(levelID, MedalType.Silver);
            if (model.IsMedalUnlocked(MedalType.Gold)) data.UnlockMedal(levelID, MedalType.Gold);
        }

        private void SaveNewBest(in LevelWonModel model, ref SaveData data)
        {
            if (model.NewRecord)
            {
                data.SetNewBestTime(CurrentLevel, model.BestTime);
            }
        }

        public override void Dispose()
        {
            continueButton.onClick.RemoveAllListeners();
            retryButton.onClick.RemoveAllListeners();
            hubButton.onClick.RemoveAllListeners();
            onBegin.RemoveAllListeners();
            
            base.Dispose();
        }
    }
}