using System;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.Utils;
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
        public int CurrentLevel => LevelManager.GetLevelID();
        
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
            hubButton.onClick.AddListener(HubHandler);
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

        private void HubHandler()
        {
            SceneHandler.LoadHub();
        }

        private void RetryLevelHandler()
        {
            SceneHandler.ReloadCurrent();
        }

        private void OnLoadNextHandler()
        {
            var sceneCount = SceneHandler.GetSceneCount();
            var currentIndex = SceneHandler.GetCurrentSceneIndex();

            var sceneToLoad = SceneHandler.HubIndex;
            
            if (currentIndex < sceneCount - 1)
            {
                sceneToLoad = currentIndex + 1;
            }

            SceneHandler.LoadSceneAsync(sceneToLoad);
        }

        private void CheckTime(in LevelWonModel model)
        {
            if (model.LevelWon) return;
            
            continueButton.interactable = false;
        }
        
        private void SetModelValues(in LevelWonModel model)
        {
            var data = SaveSystem.LoadGame();
            var endTime = LevelManager.LevelCompleteTime();
            data.CheckBestTime(CurrentLevel, endTime, out var prevBest, out var currBest, out var newRecord);

            var bronze = LevelManager.GetMedalInfo(MedalType.Bronze);
            var silver = LevelManager.GetMedalInfo(MedalType.Silver);
            var gold = LevelManager.GetMedalInfo(MedalType.Gold);

            model.Initialize(endTime, currBest, newRecord, bronze, silver, gold);
        }

        private void UpdateSaveData(in LevelWonModel model)
        {
            var data = SaveSystem.LoadGame();
            
            SaveUnlockedMedals(model, ref data);
            SaveNewBest(model, ref data);
            
            data.SaveGame();
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

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
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
        
        public override bool TryGetState(out UIState state)
        {
            state = new LevelWonState(this);
            return true;
        }
    }
}