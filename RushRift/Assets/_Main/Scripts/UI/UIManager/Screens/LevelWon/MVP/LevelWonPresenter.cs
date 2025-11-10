using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
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

namespace Game.UI.StateMachine
{
    public sealed class LevelWonPresenter : UIPresenter<LevelWonModel, LevelWonView>
    {
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hubButton;

        [Header("Events")]
        [SerializeField] private UnityEvent onBegin = new UnityEvent();

        private bool _begun;
        private ActionObserver<bool> _loadingObserver;
        
        private void Awake()
        {
            continueButton.onClick.AddListener(OnLoadNextHandler);
            retryButton.onClick.AddListener(RetryLevelHandler);
            hubButton.onClick.AddListener(HubHandler);

            _loadingObserver = new ActionObserver<bool>((a) => { _begun = false; });

            GameEntry.LoadingState.AttachOnLoading(_loadingObserver);
        }
        
        public override void Begin()
        {
            if (_begun) return;
            _begun = true;
            base.Begin();
            
            // Set Cursor
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
            

            //Model.Reset();
            
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
            continueButton.interactable = model.LevelWon;
            
            EventSystem.current.SetSelectedGameObject(null);
            // ToDo: Check if the player is playing with a game pad.
            //EventSystem.current.SetSelectedGameObject(model.LevelWon ? continueButton.gameObject : retryButton.gameObject);
        }
        
        private void SetModelValues(in LevelWonModel model)
        {
            var data = SaveSystem.LoadGame();
            var endTime = GlobalLevelManager.CompleteTime;
            var id = GlobalLevelManager.GetID();
            
            data.CheckBestTime(id, endTime, out var prevBest, out var currBest, out var newRecord);

            var medalInfo = new Dictionary<MedalType, MedalInfo>();

            if (GlobalLevelManager.TryGetMedalInfo(MedalType.Bronze, out var medal))
            {
                medalInfo[MedalType.Bronze] = medal;
            }
            
            if (GlobalLevelManager.TryGetMedalInfo(MedalType.Silver, out medal))
            {
                medalInfo[MedalType.Silver] = medal;
            }
            
            if (GlobalLevelManager.TryGetMedalInfo(MedalType.Gold, out medal))
            {
                medalInfo[MedalType.Gold] = medal;
            }
            
            model.Initialize(endTime, currBest, newRecord, medalInfo);
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
            if (_loadingObserver != null)
            {
                _loadingObserver.Dispose();
                GameEntry.LoadingState.DetachOnLoading(_loadingObserver);
            }
            
            
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