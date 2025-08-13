using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Screens
{
    public class LevelWonView : UIView
    {
        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text current;
        [SerializeField] private TMP_Text best;
        [SerializeField] private GameObject newBest;

        protected override void Awake()
        {
            base.Awake();
            animator.enabled = false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            animator.enabled = true;

            var data = SaveAndLoad.Load();
            if (data.BestTimes.TryGetValue(SceneManager.GetActiveScene().buildIndex, out var bestTime))
            {
                var time = LevelManager.LevelCompleteTime();
                
                if (time <= bestTime) newBest.SetActive(true);
            }



        }
    }
}