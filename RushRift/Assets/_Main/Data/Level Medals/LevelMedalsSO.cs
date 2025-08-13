using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Medal", menuName = "Medals", order = 2)]
public class LevelMedalsSO : ScriptableObject
{
    public int levelNumber;
    public medalTimes levelMedalTimes;
}

[System.Serializable]
public struct medal
{
    public bool isAcquired;
    public float time;
}

[System.Serializable]
public struct medalTimes
{
    public medal bronze;
    public medal silver;
    public medal gold;
}

