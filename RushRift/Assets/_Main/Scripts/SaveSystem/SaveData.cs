using Game.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[System.Serializable]
public class SaveData
{
    public int playerCurrency;

    public Dictionary<int, float> BestTimes
    {
        get => LevelBestTimes ??= new Dictionary<int, float>();
        private set => LevelBestTimes = value;
    }
    
    public Dictionary<int, bool> UnlockedEffects = new();
    public Dictionary<int, float> LevelBestTimes = new();
    public CameraSettings camera = new(.35f, 30); 
    
    public List<int> GetActiveEffects()
    {
        if (UnlockedEffects == null) return null;
        List<int> effects = new List<int>();
        foreach (var item in UnlockedEffects)
        {
            if (item.Value == true) effects.Add(item.Key);
        }

        return effects;
    }
}


[System.Serializable]
public struct CameraSettings
{
    public float Sensibility;
    public float Smoothness;
    public bool InvertX;
    public bool InvertY;
    
    public CameraSettings(float sensibility, float smoothness, bool invertX = false, bool invertY = false)
    {
        Sensibility = sensibility;
        Smoothness = smoothness;
        InvertX = invertX;
        InvertY = invertY;
    }
}


