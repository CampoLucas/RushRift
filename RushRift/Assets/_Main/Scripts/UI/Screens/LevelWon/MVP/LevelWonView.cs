using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class LevelWonView : UIView
    {
        [SerializeField] private Animator animator;
        [SerializeField] private UnityEvent onShow = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            animator.enabled = false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            //animator.enabled = true;

            onShow.Invoke();
            BestTime();
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

        
    }
}