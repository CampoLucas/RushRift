using System.Collections.Generic;

[System.Serializable]
public class ScoreData
{
    public Dictionary<int, Dictionary<string, Score>> leaderBoardList = new Dictionary<int, Dictionary<string, Score>>();
}

[System.Serializable]
public struct Score
{
    public string name;
    public string id;
    public float timescore;
}
