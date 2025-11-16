using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.DataBase;
using Game;
using System.Threading;
using TMPro;
using Game.UI.StateMachine;
using UnityEngine.EventSystems;

public class LeaderboardPresenter : UIPresenter<LeaderboardModel, LeaderboardView>
{
    [SerializeField] private List<GameObject> userNameList;
    [SerializeField] private List<GameObject> userTimelist;

    private bool hasChecked;

    public void Init()
    {
        for (int i = 0; i < userNameList.Count; i++)
        {
            userNameList[i].SetActive(false);
            userTimelist[i].SetActive(false);
        }

        hasChecked = false;
    }

    public override void Begin()
    {
        
        base.Begin();
    }

    public void GetScoreData()
    {
        if (hasChecked) return;
        CancellationTokenSource cts = new CancellationTokenSource();
        var id = GlobalLevelManager.GetID();
        DataBaseHandler.DB.GetScore(id,OnrecievedScore,cts.Token);
        hasChecked = true;
    }

    private void OnrecievedScore(ScoreList scoreList)
    {
        
        for (int i = 0; i < scoreList.scores.Length; i++)
        {
            userNameList[i].SetActive(true);
            userTimelist[i].SetActive(true);

            var userNameText = userNameList[i].GetComponentInChildren<TMP_Text>();
            var userTimeText = userTimelist[i].GetComponentInChildren<TMP_Text>();

            userNameText.text = scoreList.scores[i].name;
            userTimeText.text = scoreList.scores[i].timescore;
        }
        
    }

    public override bool TryGetState(out UIState state)
    {
        state = new LeaderboardState(this);
        return true;
    }
    
    public override void Dispose()
    {
        base.Dispose();
    }

    public override void End()
    {
        EventSystem.current.SetSelectedGameObject(null);
        base.End();
    }

}
