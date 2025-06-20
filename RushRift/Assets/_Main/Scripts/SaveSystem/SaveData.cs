using Game.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[System.Serializable]
public class SaveData
{
    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the BestTime dictionary, it creates it.
    /// </summary>
    public Dictionary<int, float> BestTimes
    {
        get => _bestTimes ??= new Dictionary<int, float>();
        private set => _bestTimes = value;
    }

    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the UnlockedEffects dictionary, it creates it.
    /// </summary>
    public Dictionary<int, bool> UnlockedEffects
    {
        get => _unlockedEffects ??= new Dictionary<int, bool>();
        private set => _unlockedEffects = value;
    }
    
    public CameraSettings camera = new(.35f, 30); 
    public int playerCurrency;
    
    private Dictionary<int, bool> _unlockedEffects = new();
    private Dictionary<int, float> _bestTimes = new();
    
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


