using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.DataBase;
using Game;
using System.Threading;
using TMPro;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private GameObject userNameObj;
    [SerializeField] private GameObject userTimeObj;
    [SerializeField] private Transform userNameLayout;
    [SerializeField] private Transform userTimeLayout;
    private bool hasChecked;

    public void Init()
    {
        hasChecked = false;
    }

    public void GetScoreData()
    {
        DataBaseHandler.Init();//cambiarlo de lugar, solo esta aca para prueba
        CancellationTokenSource cts = new CancellationTokenSource();
        var id = GlobalLevelManager.GetID();
        DataBaseHandler.DB.GetScore(id,OnrecievedScore,cts.Token);
    }

    private void OnrecievedScore(ScoreList scoreList)
    {
        if (hasChecked) return;
        for (int i = 0; i < scoreList.scores.Length; i++)
        {
            var newUserName = Instantiate(userNameObj, userNameLayout);
            var newUserTime = Instantiate(userTimeObj, userTimeLayout);
            var userNameText = newUserName.GetComponentInChildren<TMP_Text>();
            var userTimeText = newUserTime.GetComponentInChildren<TMP_Text>();

            userNameText.text = scoreList.scores[i].name;
            userTimeText.text = scoreList.scores[i].timescore;
        }
        hasChecked = true;
    }

  
}
