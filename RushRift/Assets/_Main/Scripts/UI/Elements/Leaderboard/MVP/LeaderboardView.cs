using Game.DesignPatterns.Observers;
using TMPro;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public class LeaderboardView : UIView
    {
        [SerializeField] private GameObject loading;
        [SerializeField] private GameObject error;
        [SerializeField] private GameObject leaderboard;

        [SerializeField] private Transform layoutParent;
        [SerializeField] private GameObject userNameObj;
        [SerializeField] private GameObject userTimeObj;
        [SerializeField] private GameObject userPosObj;

        private bool hasChecked;

        private void Awake()
        {
            hasChecked = false;
        }

        public void OnSuccessHandler(ScoreList scoreList)
        {
            if (hasChecked) return;
            for (int i = 0; i < scoreList.scores.Length; i++)
            {
                var newUserName = Instantiate(userNameObj, layoutParent);
                var newUserTime = Instantiate(userTimeObj, layoutParent);
                var userNameText = newUserName.GetComponentInChildren<TMP_Text>();
                var userTimeText = newUserTime.GetComponentInChildren<TMP_Text>();

                userNameText.text = scoreList.scores[i].name;
                userTimeText.text = scoreList.scores[i].timescore;
            }
            hasChecked = true;

        }

        public void OnLoadingHandler()
        {

        }

        public void OnFailureHandler()
        {

        }
    }



}


