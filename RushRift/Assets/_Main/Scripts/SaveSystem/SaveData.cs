using Game.Entities;
using System.Collections.Generic;
using UnityEngine.Serialization;


[System.Serializable]
public class SaveData
{
    public int playerCurrency;
    public Dictionary<int, bool> unlockedEffects = new();
    public Dictionary<int, string> levelBestTimes = new();
    public CameraSettings Camera = new CameraSettings(.35f, 30);

    
    
    public List<int> GetActiveEffects()
    {
        if (unlockedEffects == null) return null;
        List<int> effects = new List<int>();
        foreach (var item in unlockedEffects)
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


