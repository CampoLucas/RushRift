using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;
using System.Threading;
using TMPro;
using Game.UI.StateMachine;
using UnityEngine.EventSystems;
using Game.Saves;

public class LeaderboardPresenter : UIPresenter<LeaderboardModel, LeaderboardView>
{
    [SerializeField] private int rankLimit;
    [SerializeField] private List<GameObject> userNameList;
    [SerializeField] private List<GameObject> userTimelist;
    [SerializeField] private List<GameObject> userPoslist;

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
        //if (hasChecked) return;
        var leaderBoardSave = SaveSystem.LoadLeaderBoard();
        OnrecievedScore(leaderBoardSave);
        //hasChecked = true;

    }

    private void OnrecievedScore(LeaderBoardData leaderBoardData)
    {
        ResetLeaderboard();
        var levelLeaderBoard = leaderBoardData.GetLevelLeaderBoard(GlobalLevelManager.GetID());

        for (int i = 0; i < levelLeaderBoard.Count; i++)
        {
            userNameList[i].SetActive(true);
            userTimelist[i].SetActive(true);
            userPoslist[i].SetActive(true);

            var userNameText = userNameList[i].GetComponentInChildren<TMP_Text>();
            var userTimeText = userTimelist[i].GetComponentInChildren<TMP_Text>();
            var userPosText = userPoslist[i].GetComponentInChildren<TMP_Text>();

            var newFormatedTime = FormatTime(levelLeaderBoard[i].timescore);

            userNameText.text = levelLeaderBoard[i].name;
            userTimeText.text = newFormatedTime.ToString();
            userPosText.text = (i + 1).ToString();
            if (i == (rankLimit-1)) break;
        }

    }

    private void ResetLeaderboard()
    {
        for (int i = 0; i < userNameList.Count; i++)
        {
            userNameList[i].SetActive(false);
            userTimelist[i].SetActive(false);
            userPoslist[i].SetActive(false);
        }
    }

    private string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60f);
        var seconds = Mathf.FloorToInt(time % 60f);
        var milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
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
