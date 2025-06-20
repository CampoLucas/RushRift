using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class LevelWonPresenter : UIPresenter<LevelWonModel, LevelWonView>
    {
        [SerializeField] private Button continueButton;

        public override void Begin()
        {
            base.Begin();
            
            EventSystem.current.SetSelectedGameObject(null);
            
            // Set Cursor
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        public override void End()
        {
            base.End();
            
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void Awake()
        {
            continueButton.onClick.AddListener(OnLoadNextHandler);
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

        private void OnDestroy()
        {
            continueButton.onClick.RemoveAllListeners();
        }
    }
}