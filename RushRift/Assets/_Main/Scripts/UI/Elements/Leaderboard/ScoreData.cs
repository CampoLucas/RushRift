using System.Collections.Generic;

[System.Serializable]
public class ScoreData
{
    public Dictionary<int,List<Score>> leaderBoardList = new Dictionary<int,List<Score>>();
}

[System.Serializable]
public struct Score
{
    public string name;
    public float timescore;
}
