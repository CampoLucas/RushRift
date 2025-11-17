using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.DataBase;
using Game;
using System.Threading;
using Game.DesignPatterns.Observers;
using TMPro;
using Game.UI.StateMachine;
using UnityEngine.EventSystems;

public class LeaderboardPresenter : UIPresenter<LeaderboardModel, LeaderboardView>
{
    [SerializeField] private List<GameObject> userNameList;
    [SerializeField] private List<GameObject> userTimelist;

    public Subject<LeaderboardParams> OnSuccess; //change the params later
    public Subject<DBRequestState> OnFailure;
    public Subject OnLoading;

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

    
    public async void GetScoreData()
    {
        if (hasChecked) return;
        hasChecked = true;

        CancellationTokenSource cts = new CancellationTokenSource();
        var id = GlobalLevelManager.GetID();
        
        GetScoreDataAsync(id, cts);
    }

    public async void GetScoreDataAsync(int id, CancellationTokenSource cts)
    {
        try
        {
            var state = await DataBaseHandler.DB.GetScore(id, OnrecievedScore, cts.Token);
            // Set loading screen here
            if (state != DBRequestState.Success)
            {
                OnFailure.NotifyAll(state);
            }
            else
            {
                OnSuccess.NotifyAll(new LeaderboardParams());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            OnFailure.NotifyAll(DBRequestState.Unknown); // Set the error window
        }
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
