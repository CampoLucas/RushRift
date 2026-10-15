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
        if (hasChecked) return;
        //Checkear la lista en el save
        var leaderBoardSave = SaveSystem.LoadLeaderBoard();

        hasChecked = true;

    }

    private void OnrecievedScore(ScoreData scoreList)
    {

        //for (int i = 0; i < scoreList.leaderBoardList.Count; i++)
        //{
        //    userNameList[i].SetActive(true);
        //    userTimelist[i].SetActive(true);
        //    userPoslist[i].SetActive(true);

        //    var userNameText = userNameList[i].GetComponentInChildren<TMP_Text>();
        //    var userTimeText = userTimelist[i].GetComponentInChildren<TMP_Text>();
        //    var userPosText = userPoslist[i].GetComponentInChildren<TMP_Text>();

        //    userNameText.text = scoreList.leaderBoardList[i].;
        //    userTimeText.text = scoreList.leaderBoardList[i].timescore;
        //    userPosText.text = (i + 1).ToString();
        //}

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
