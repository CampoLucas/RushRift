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
            
            // Set Cursor
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
            //OnWinLevel();
            EventSystem.current.SetSelectedGameObject(null);
            
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
            UIManager.Instance.Get().LoadHUB();
        }

        private void RetryLevelHandler()
        {
            UIManager.Instance.Get().Restart();
        }

        private void OnLoadNextHandler()
        {
            // ToDo: make a LevelWon variant screen for when finishing all levels
            GlobalLevelManager.LoadNextLevelAsync();
        }

        private void CheckTime(in LevelWonModel model)
        {
            if (model.LevelWon) return;
            
            continueButton.interactable = false;
        }
        
        private void SetModelValues(in LevelWonModel model)
        {
            var data = SaveSystem.LoadGame();
            var endTime = GlobalLevelManager.CompleteTime;
            data.CheckBestTime(GlobalLevelManager.GetID(), endTime, out var prevBest, out var currBest, out var newRecord);

            var bronze = GlobalLevelManager.GetMedalInfo(MedalType.Bronze);
            var silver = GlobalLevelManager.GetMedalInfo(MedalType.Silver);
            var gold = GlobalLevelManager.GetMedalInfo(MedalType.Gold);

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
            var levelID = GlobalLevelManager.GetID();
            
            if (model.IsMedalUnlocked(MedalType.Bronze)) data.UnlockMedal(levelID, MedalType.Bronze);
            if (model.IsMedalUnlocked(MedalType.Silver)) data.UnlockMedal(levelID, MedalType.Silver);
            if (model.IsMedalUnlocked(MedalType.Gold)) data.UnlockMedal(levelID, MedalType.Gold);
        }

        private void SaveNewBest(in LevelWonModel model, ref SaveData data)
        {
            if (model.NewRecord)
            {
                data.SetNewBestTime(GlobalLevelManager.GetID(), model.BestTime);
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